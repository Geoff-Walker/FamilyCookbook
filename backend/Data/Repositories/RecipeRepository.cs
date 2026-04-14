using Microsoft.EntityFrameworkCore;
using WalkerFcb.Api.Data.Entities;
using WalkerFcb.Api.DTOs;

namespace WalkerFcb.Api.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRecipeRepository"/>.
/// Injected as scoped via DI — one instance per HTTP request.
/// </summary>
public class RecipeRepository : IRecipeRepository
{
    private readonly WalkerDbContext _db;

    public RecipeRepository(WalkerDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<List<RecipeSummary>> GetAllAsync()
    {
        return await _db.Recipes
            .AsNoTracking()
            .Select(r => new RecipeSummary
            {
                Id = r.Id,
                Title = r.Title,
                Description = r.Description,
                PrepTimeMinutes = r.PrepTimeMinutes,
                CookTimeMinutes = r.CookTimeMinutes,
                Servings = r.Servings,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                ImageUrl = r.ImageUrl,
                AverageRating = r.Reviews.Count == 0
                    ? (double?)null
                    : r.Reviews.Average(rv => (double)rv.Rating),
                ReviewCount = r.Reviews.Count,
                Tags = r.RecipeTags
                    .OrderBy(rt => rt.Tag.Category.Name)
                    .ThenBy(rt => rt.Tag.Name)
                    .Select(rt => new RecipeTagSummary
                    {
                        TagId = rt.TagId,
                        TagName = rt.Tag.Name,
                        TagSlug = rt.Tag.Slug,
                        CategoryName = rt.Tag.Category.Name
                    })
                    .ToList(),
                PerUserRatings = r.Reviews
                    .GroupBy(rv => new { rv.UserId, rv.User.Name })
                    .Select(g => new RecipeSummaryRatingDto
                    {
                        UserId = g.Key.UserId,
                        UserName = g.Key.Name,
                        AverageRating = g.Average(rv => (double)rv.Rating)
                    })
                    .ToList()
            })
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Recipe?> GetByIdAsync(int id)
    {
        return await _db.Recipes
            .Include(r => r.Stages)
                .ThenInclude(s => s.Steps)
            .Include(r => r.Stages)
                .ThenInclude(s => s.Ingredients)
                    .ThenInclude(ri => ri.Ingredient)
            .Include(r => r.Stages)
                .ThenInclude(s => s.Ingredients)
                    .ThenInclude(ri => ri.Unit)
            .Include(r => r.Ingredients)
                .ThenInclude(ri => ri.Ingredient)
            .Include(r => r.Ingredients)
                .ThenInclude(ri => ri.Unit)
            .Include(r => r.RecipeTags)
                .ThenInclude(rt => rt.Tag)
                    .ThenInclude(t => t.Category)
            .Include(r => r.Reviews)
                .ThenInclude(rv => rv.User)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    /// <inheritdoc />
    public async Task<Recipe> CreateAsync(Recipe recipe)
    {
        _db.Recipes.Add(recipe);
        await _db.SaveChangesAsync();
        return recipe;
    }

    /// <inheritdoc />
    public async Task<Recipe> UpdateAsync(Recipe recipe)
    {
        _db.Recipes.Update(recipe);
        await _db.SaveChangesAsync();
        return recipe;
    }

    /// <inheritdoc />
    public async Task SoftDeleteAsync(int id)
    {
        var recipe = await _db.Recipes.FindAsync(id)
            ?? throw new KeyNotFoundException($"Recipe {id} not found.");

        recipe.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
