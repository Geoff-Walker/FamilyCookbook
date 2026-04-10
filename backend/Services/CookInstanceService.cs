using Microsoft.EntityFrameworkCore;
using WalkerFcb.Api.Data;
using WalkerFcb.Api.Data.Entities;
using WalkerFcb.Api.DTOs;

namespace WalkerFcb.Api.Services;

/// <summary>
/// Business logic for the cook instance lifecycle:
/// start, retrieve, patch ingredients, complete, soft-delete, and list history.
/// </summary>
public class CookInstanceService
{
    private readonly WalkerDbContext _db;

    // Rating must be a multiple of 0.5 in the range [0, 5].
    private static readonly HashSet<decimal> ValidRatings =
        Enumerable.Range(0, 11).Select(i => i * 0.5m).ToHashSet();

    public CookInstanceService(WalkerDbContext db)
    {
        _db = db;
    }

    // -----------------------------------------------------------------------
    // START COOK
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates a new cook instance for the given recipe and user.
    /// Copies all recipe_ingredients into cook_instance_ingredients.
    /// Returns null if the recipe or user is not found.
    /// </summary>
    public async Task<CookInstanceDetailDto?> StartCookAsync(StartCookDto request)
    {
        // Verify recipe exists (query filter excludes soft-deleted)
        var recipe = await _db.Recipes
            .Include(r => r.Stages.OrderBy(s => s.SortOrder))
                .ThenInclude(s => s.Ingredients.OrderBy(i => i.SortOrder))
                    .ThenInclude(i => i.Ingredient)
            .Include(r => r.Stages)
                .ThenInclude(s => s.Ingredients)
                    .ThenInclude(i => i.Unit)
            .Include(r => r.Ingredients.Where(i => i.StageId == null).OrderBy(i => i.SortOrder))
                .ThenInclude(i => i.Ingredient)
            .Include(r => r.Ingredients.Where(i => i.StageId == null))
                .ThenInclude(i => i.Unit)
            .FirstOrDefaultAsync(r => r.Id == request.RecipeId);

        if (recipe == null)
            return null;

        // Verify user exists
        var user = await _db.Users.FindAsync(request.UserId);
        if (user == null)
            return null;

        var cookInstance = new CookInstance
        {
            RecipeId = request.RecipeId,
            UserId = request.UserId,
            StartedAt = DateTime.UtcNow,
            Portions = request.Portions,
            Notes = request.Notes
        };

        // Collect all recipe_ingredients and copy into cook_instance_ingredients.
        // Gather both stage-scoped and whole-recipe ingredients.
        var allRecipeIngredients = recipe.Stages
            .SelectMany(s => s.Ingredients)
            .Concat(recipe.Ingredients.Where(i => i.StageId == null))
            .ToList();

        foreach (var ri in allRecipeIngredients)
        {
            // Parse recipe ingredient amount — stored as text; default 0 if unparseable
            decimal.TryParse(ri.Amount, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var parsedAmount);

            cookInstance.Ingredients.Add(new CookInstanceIngredient
            {
                IngredientId = ri.IngredientId,
                Amount = parsedAmount,
                UnitId = ri.UnitId,
                Checked = false,
                IsLimiter = false,
                Notes = ri.Notes
            });
        }

        _db.CookInstances.Add(cookInstance);
        await _db.SaveChangesAsync();

        // Re-load with full navigation for projection
        return await LoadDetailAsync(cookInstance.Id);
    }

    // -----------------------------------------------------------------------
    // GET BY ID
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns the full cook instance detail DTO, or null if not found or soft-deleted.
    /// </summary>
    public async Task<CookInstanceDetailDto?> GetByIdAsync(int id)
    {
        return await LoadDetailAsync(id);
    }

    // -----------------------------------------------------------------------
    // PATCH INGREDIENT
    // -----------------------------------------------------------------------

    /// <summary>
    /// Applies a partial update to a cook instance ingredient.
    /// Returns false if the cook instance or ingredient is not found.
    /// </summary>
    public async Task<bool> PatchIngredientAsync(int cookInstanceId, int ingredientId, PatchCookInstanceIngredientDto patch)
    {
        var cookExists = await _db.CookInstances
            .AnyAsync(ci => ci.Id == cookInstanceId && ci.DeletedAt == null);

        if (!cookExists)
            return false;

        var ingredient = await _db.CookInstanceIngredients
            .FirstOrDefaultAsync(cii =>
                cii.Id == ingredientId &&
                cii.CookInstanceId == cookInstanceId);

        if (ingredient == null)
            return false;

        if (patch.Checked.HasValue)
            ingredient.Checked = patch.Checked.Value;

        if (patch.Amount.HasValue)
            ingredient.Amount = patch.Amount.Value;

        if (patch.IsLimiter.HasValue)
            ingredient.IsLimiter = patch.IsLimiter.Value;

        await _db.SaveChangesAsync();
        return true;
    }

    // -----------------------------------------------------------------------
    // COMPLETE
    // -----------------------------------------------------------------------

    /// <summary>
    /// Marks a cook instance as complete and optionally submits reviews.
    /// Returns null if not found/deleted. Returns a validation error string
    /// if a rating fails the {0, 0.5, ..., 5.0} constraint.
    /// </summary>
    public async Task<(CookInstanceDetailDto? Dto, string? ValidationError)> CompleteCookAsync(
        int id, CompleteCookDto request)
    {
        // Validate ratings before touching the DB
        foreach (var review in request.Reviews)
        {
            if (!ValidRatings.Contains(review.Rating))
                return (null, $"rating {review.Rating} is invalid — must be a multiple of 0.5 between 0 and 5");
        }

        var cookInstance = await _db.CookInstances
            .FirstOrDefaultAsync(ci => ci.Id == id && ci.DeletedAt == null);

        if (cookInstance == null)
            return (null, null);

        cookInstance.CompletedAt = DateTime.UtcNow;

        if (request.Portions.HasValue)
            cookInstance.Portions = request.Portions;

        if (request.Notes != null)
            cookInstance.Notes = request.Notes;

        // Persist any submitted reviews
        foreach (var reviewDto in request.Reviews)
        {
            _db.RecipeReviews.Add(new RecipeReview
            {
                RecipeId = cookInstance.RecipeId,
                UserId = reviewDto.UserId,
                Rating = reviewDto.Rating,
                Notes = reviewDto.Notes,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        var dto = await LoadDetailAsync(id);
        return (dto, null);
    }

    // -----------------------------------------------------------------------
    // SOFT DELETE
    // -----------------------------------------------------------------------

    /// <summary>
    /// Soft-deletes a cook instance. Returns false if not found or already deleted.
    /// </summary>
    public async Task<bool> SoftDeleteAsync(int id)
    {
        var cookInstance = await _db.CookInstances
            .FirstOrDefaultAsync(ci => ci.Id == id && ci.DeletedAt == null);

        if (cookInstance == null)
            return false;

        cookInstance.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    // -----------------------------------------------------------------------
    // HISTORY LIST
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns all non-deleted cook instances for a recipe, ordered by started_at DESC.
    /// Returns null if the recipe does not exist.
    /// </summary>
    public async Task<List<CookInstanceSummaryDto>?> GetHistoryByRecipeAsync(int recipeId)
    {
        // Verify recipe exists (query filter excludes soft-deleted)
        var recipeExists = await _db.Recipes.AnyAsync(r => r.Id == recipeId);
        if (!recipeExists)
            return null;

        // Load cook instances for this recipe with their associated reviews
        var cookInstances = await _db.CookInstances
            .AsNoTracking()
            .Where(ci => ci.RecipeId == recipeId && ci.DeletedAt == null)
            .Include(ci => ci.User)
            .OrderByDescending(ci => ci.StartedAt)
            .ToListAsync();

        var cookInstanceIds = cookInstances.Select(ci => ci.Id).ToList();

        // Load reviews for these cook instances via the recipe
        // Reviews are linked to recipes, not cook instances directly.
        // We can't join reviews to cook instances — reviews sit on RecipeReview
        // without a cook_instance_id FK at this stage. Return empty review list.
        // (The WAL-71 history AC only requires date, portions, notes per row — reviews
        // are surfaced in the detail view, not the history list summary.)

        return cookInstances.Select(ci => new CookInstanceSummaryDto
        {
            Id = ci.Id,
            UserId = ci.UserId,
            UserName = ci.User.Name,
            StartedAt = ci.StartedAt,
            CompletedAt = ci.CompletedAt,
            Portions = ci.Portions,
            Notes = ci.Notes,
            Reviews = []
        }).ToList();
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Loads a cook instance by ID (excluding soft-deleted) with all navigation
    /// properties needed for the detail DTO projection.
    /// </summary>
    private async Task<CookInstanceDetailDto?> LoadDetailAsync(int id)
    {
        var ci = await _db.CookInstances
            .AsNoTracking()
            .Where(c => c.Id == id && c.DeletedAt == null)
            .Include(c => c.Recipe)
                .ThenInclude(r => r.Stages.OrderBy(s => s.SortOrder))
            .Include(c => c.User)
            .Include(c => c.Ingredients)
                .ThenInclude(cii => cii.Ingredient)
            .Include(c => c.Ingredients)
                .ThenInclude(cii => cii.Unit)
            .FirstOrDefaultAsync();

        if (ci == null)
            return null;

        // Build a stage lookup: ingredientId (from recipe_ingredients) → stage info.
        // We need to know which stage each cook_instance_ingredient belongs to.
        // cook_instance_ingredients don't directly store stage_id; we infer it from
        // recipe_ingredients by matching ingredient_id.
        //
        // Note: if a recipe has the same ingredient in multiple stages, we match
        // the first occurrence in stage sort_order — a known limitation acceptable
        // at this phase.

        var recipeIngredients = await _db.RecipeIngredients
            .AsNoTracking()
            .Where(ri => ri.RecipeId == ci.RecipeId)
            .Include(ri => ri.Stage)
            .OrderBy(ri => ri.StageId.HasValue ? 1 : 0)  // staged before unstaged
            .ThenBy(ri => ri.Stage != null ? ri.Stage.SortOrder : 0)
            .ThenBy(ri => ri.SortOrder)
            .ToListAsync();

        // Map: ingredient_id → (stageId, stageName, stageSortOrder)
        // First occurrence wins for de-duplication.
        var ingredientToStage = new Dictionary<int, (int? StageId, string? StageName, int StageSortOrder)>();
        foreach (var ri in recipeIngredients)
        {
            if (!ingredientToStage.ContainsKey(ri.IngredientId))
            {
                ingredientToStage[ri.IngredientId] = (
                    ri.StageId,
                    ri.Stage?.Name,
                    ri.Stage?.SortOrder ?? int.MaxValue
                );
            }
        }

        // Group cook_instance_ingredients by stage
        var grouped = ci.Ingredients
            .GroupBy(cii =>
            {
                var stageInfo = ingredientToStage.TryGetValue(cii.IngredientId, out var s)
                    ? s
                    : (null, (string?)null, int.MaxValue);
                return stageInfo;
            })
            .OrderBy(g => (int)g.Key.StageSortOrder)
            .Select(g => new CookInstanceStageGroupDto
            {
                StageId = g.Key.StageId,
                StageName = g.Key.StageName,
                SortOrder = g.Key.StageSortOrder == int.MaxValue ? 0 : g.Key.StageSortOrder,
                Ingredients = g.Select(cii => new CookInstanceIngredientDto
                {
                    Id = cii.Id,
                    IngredientId = cii.IngredientId,
                    IngredientName = cii.Ingredient.Name,
                    Amount = cii.Amount,
                    UnitId = cii.UnitId,
                    UnitName = cii.Unit?.Name,
                    UnitAbbreviation = cii.Unit?.Abbreviation,
                    Checked = cii.Checked,
                    IsLimiter = cii.IsLimiter,
                    Notes = cii.Notes
                }).ToList()
            }).ToList();

        return new CookInstanceDetailDto
        {
            Id = ci.Id,
            RecipeId = ci.RecipeId,
            RecipeTitle = ci.Recipe.Title,
            UserId = ci.UserId,
            UserName = ci.User.Name,
            StartedAt = ci.StartedAt,
            CompletedAt = ci.CompletedAt,
            Portions = ci.Portions,
            Notes = ci.Notes,
            StageGroups = grouped
        };
    }
}
