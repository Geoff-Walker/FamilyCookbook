using Microsoft.EntityFrameworkCore;
using OpenAI;
using OpenAI.Embeddings;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using WalkerFcb.Api.Data;
using WalkerFcb.Api.Data.Entities;
using WalkerFcb.Api.DTOs;

namespace WalkerFcb.Api.Services;

/// <summary>
/// Semantic search: embeds the query via OpenAI text-embedding-3-small,
/// then ranks recipes using pgvector cosine similarity with a per-user rating boost.
/// Optionally filters results by ingredient IDs (all must match) and/or tag IDs
/// (AND across categories, OR within same category).
/// </summary>
public class SearchService
{
    private const string EmbeddingModel = "text-embedding-3-small";
    private const int ResultLimit = 10;

    private readonly WalkerDbContext _db;
    private readonly OpenAIClient? _openAi;
    private readonly ILogger<SearchService> _logger;

    public SearchService(
        WalkerDbContext db,
        OpenAIClient? openAi,
        ILogger<SearchService> logger)
    {
        _db = db;
        _openAi = openAi;
        _logger = logger;
    }

    /// <summary>
    /// Embeds <paramref name="query"/> and returns the top-ranked recipes for
    /// <paramref name="userId"/> ordered by cosine similarity * rating boost.
    /// When <paramref name="ingredientIds"/> is provided the results are restricted to
    /// recipes that contain ALL of the specified ingredients.
    /// When <paramref name="tagIds"/> is provided the results are further restricted using
    /// AND-across-categories / OR-within-category tag logic (AC9, AC11).
    /// Throws <see cref="SearchUnavailableException"/> if the OpenAI call fails.
    /// </summary>
    public async Task<List<RecipeSummaryDto>> SearchAsync(
        string query,
        int userId,
        List<int>? ingredientIds = null,
        List<int>? tagIds = null)
    {
        // 1. Guard: no OpenAI client means the key is not configured.
        if (_openAi is null)
        {
            _logger.LogWarning("Search requested but OpenAI API key is not configured.");
            throw new SearchUnavailableException("Semantic search is not available: OpenAI API key is not configured.");
        }

        // 2. If ingredient IDs are provided, resolve the set of recipe IDs that
        //    contain ALL of those ingredients. This set constrains the vector search.
        HashSet<int>? ingredientFilteredIds = null;
        if (ingredientIds is { Count: > 0 })
        {
            var matchingIds = await _db.RecipeIngredients
                .AsNoTracking()
                .Where(ri => ingredientIds.Contains(ri.IngredientId))
                .GroupBy(ri => ri.RecipeId)
                .Where(g => g.Select(ri => ri.IngredientId).Distinct().Count() == ingredientIds.Count)
                .Select(g => g.Key)
                .ToListAsync();

            ingredientFilteredIds = matchingIds.ToHashSet();

            // Short-circuit: no recipes match the ingredient filter.
            if (ingredientFilteredIds.Count == 0)
                return [];
        }

        // 2b. If tag IDs are provided, apply AND-across-categories / OR-within-category logic.
        HashSet<int>? tagFilteredIds = null;
        if (tagIds is { Count: > 0 })
        {
            var requestedTags = await _db.Tags
                .AsNoTracking()
                .Where(t => tagIds.Contains(t.Id))
                .Select(t => new { t.Id, t.CategoryId })
                .ToListAsync();

            var tagsByCategory = requestedTags
                .GroupBy(t => t.CategoryId)
                .Select(g => g.Select(t => t.Id).ToList())
                .ToList();

            HashSet<int>? accumulator = null;
            foreach (var categoryTagIds in tagsByCategory)
            {
                var recipeIdsForCategory = await _db.RecipeTags
                    .AsNoTracking()
                    .Where(rt => categoryTagIds.Contains(rt.TagId))
                    .Select(rt => rt.RecipeId)
                    .Distinct()
                    .ToListAsync();

                if (accumulator is null)
                    accumulator = recipeIdsForCategory.ToHashSet();
                else
                    accumulator.IntersectWith(recipeIdsForCategory);

                if (accumulator.Count == 0)
                    return [];
            }

            tagFilteredIds = accumulator;
        }

        // 2c. Combine ingredient and tag filter sets into a single constraint.
        HashSet<int>? combinedFilterIds = null;
        if (ingredientFilteredIds is not null && tagFilteredIds is not null)
        {
            ingredientFilteredIds.IntersectWith(tagFilteredIds);
            combinedFilterIds = ingredientFilteredIds;
            if (combinedFilterIds.Count == 0)
                return [];
        }
        else
        {
            combinedFilterIds = ingredientFilteredIds ?? tagFilteredIds;
        }

        // 3. Embed the query via OpenAI.
        float[] queryVector;
        try
        {
            var embeddingClient = _openAi.GetEmbeddingClient(EmbeddingModel);
            var response = await embeddingClient.GenerateEmbeddingAsync(query);
            queryVector = response.Value.ToFloats().ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI embedding call failed for query '{Query}'", query);
            throw new SearchUnavailableException("Embedding service unavailable.", ex);
        }

        // 4. Rank recipes by cosine similarity * per-user rating boost.
        //    Unrated recipes default to 0.6 weight.
        //    We retrieve the top N recipe IDs ordered by score, then load
        //    full data for those recipes in a separate query to keep the
        //    EF Core translation simple.
        var queryVec = new Vector(queryVector);

        var baseQuery = _db.Recipes
            .AsNoTracking()
            .Where(r => r.Embedding != null);

        // Apply combined filter constraint to the vector search when provided
        if (combinedFilterIds is not null)
            baseQuery = baseQuery.Where(r => combinedFilterIds.Contains(r.Id));

        var rankedIds = await baseQuery
            .OrderBy(r => r.Embedding!.CosineDistance(queryVec))
            .Take(ResultLimit * 3) // over-fetch before rating boost re-sort
            .Select(r => new
            {
                r.Id,
                CosineDistance = r.Embedding!.CosineDistance(queryVec),
                UserRatingAvg = r.Reviews
                    .Where(rv => rv.UserId == userId)
                    .Select(rv => (double?)rv.Rating)
                    .Average()
            })
            .ToListAsync();

        // 5. Apply rating boost in-process and take the final top N.
        var topIds = rankedIds
            .Select(r => new
            {
                r.Id,
                Score = (1.0 - r.CosineDistance)
                    * (r.UserRatingAvg.HasValue ? r.UserRatingAvg.Value / 5.0 : 0.6)
            })
            .OrderByDescending(r => r.Score)
            .Take(ResultLimit)
            .Select(r => r.Id)
            .ToList();

        if (topIds.Count == 0)
            return [];

        // 6. Load full recipe data for the ranked IDs.
        var recipes = await _db.Recipes
            .AsNoTracking()
            .Include(r => r.RecipeTags)
                .ThenInclude(rt => rt.Tag)
                    .ThenInclude(t => t.Category)
            .Include(r => r.Reviews)
                .ThenInclude(rv => rv.User)
            .Where(r => topIds.Contains(r.Id))
            .ToListAsync();

        // 7. Return in ranked order, mapped to summary DTOs.
        return topIds
            .Select(id => recipes.First(r => r.Id == id))
            .Select(r => new RecipeSummaryDto
            {
                Id = r.Id,
                Title = r.Title,
                PrepTimeMinutes = r.PrepTimeMinutes,
                CookTimeMinutes = r.CookTimeMinutes,
                ImageUrl = r.ImageUrl,
                Tags = r.RecipeTags
                    .OrderBy(rt => rt.Tag.Category.Name)
                    .ThenBy(rt => rt.Tag.Name)
                    .Select(rt => new RecipeSummaryTagDto
                    {
                        Id = rt.TagId,
                        Name = rt.Tag.Name,
                        CategoryName = rt.Tag.Category.Name
                    })
                    .ToList(),
                Ratings = r.Reviews
                    .GroupBy(rv => new { rv.UserId, rv.User.Name })
                    .Select(g => new RecipeSummaryRatingDto
                    {
                        UserId = g.Key.UserId,
                        UserName = g.Key.Name,
                        AverageRating = g.Average(rv => (double)rv.Rating)
                    })
                    .ToList()
            })
            .ToList();
    }
}

/// <summary>
/// Thrown when the search backend (OpenAI embedding) is unavailable.
/// The endpoint maps this to HTTP 503.
/// </summary>
public class SearchUnavailableException : Exception
{
    public SearchUnavailableException(string message, Exception? inner = null)
        : base(message, inner) { }
}
