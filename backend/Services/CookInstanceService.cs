using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WalkerFcb.Api.Data;
using WalkerFcb.Api.Data.Entities;
using WalkerFcb.Api.DTOs;

namespace WalkerFcb.Api.Services;

/// <summary>
/// Business logic for the cook instance lifecycle:
/// start, retrieve, patch ingredients, complete, soft-delete, list history, and promote.
/// </summary>
public class CookInstanceService
{
    private readonly WalkerDbContext _db;
    private readonly ILogger<CookInstanceService> _logger;

    // Rating must be a multiple of 0.5 in the range [0, 5].
    private static readonly HashSet<decimal> ValidRatings =
        Enumerable.Range(0, 11).Select(i => i * 0.5m).ToHashSet();

    public CookInstanceService(WalkerDbContext db, ILogger<CookInstanceService> logger)
    {
        _db = db;
        _logger = logger;
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
            .Include(r => r.Stages.OrderBy(s => s.SortOrder))
                .ThenInclude(s => s.Ingredients.OrderBy(i => i.SortOrder))
                    .ThenInclude(i => i.Unit)
            .Include(r => r.Ingredients.Where(i => i.StageId == null).OrderBy(i => i.SortOrder))
                .ThenInclude(i => i.Ingredient)
            .Include(r => r.Ingredients.Where(i => i.StageId == null).OrderBy(i => i.SortOrder))
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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        foreach (var reviewDto in request.Reviews)
        {
            _db.RecipeReviews.Add(new RecipeReview
            {
                RecipeId = cookInstance.RecipeId,
                UserId = reviewDto.UserId,
                Rating = reviewDto.Rating,
                Notes = reviewDto.Notes,
                MadeOn = today,
                CookInstanceId = cookInstance.Id,
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
    /// Returns all non-deleted cook instances for a recipe, ordered by started_at DESC,
    /// along with the original recipe date for rendering the baseline "Original Recipe" row.
    /// Returns null if the recipe does not exist.
    /// </summary>
    public async Task<CookHistoryResponseDto?> GetHistoryByRecipeAsync(int recipeId)
    {
        // Verify recipe exists (query filter excludes soft-deleted)
        var recipe = await _db.Recipes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == recipeId);
        if (recipe == null)
            return null;

        // Load cook instances for this recipe with their associated reviews
        var cookInstances = await _db.CookInstances
            .AsNoTracking()
            .Where(ci => ci.RecipeId == recipeId && ci.DeletedAt == null)
            .Include(ci => ci.User)
            .OrderByDescending(ci => ci.StartedAt)
            .ToListAsync();

        var cookInstanceIds = cookInstances.Select(ci => ci.Id).ToList();

        // Load all reviews for these cook instances via the CookInstanceId FK.
        var reviews = await _db.RecipeReviews
            .AsNoTracking()
            .Where(rr => rr.CookInstanceId != null && cookInstanceIds.Contains(rr.CookInstanceId!.Value))
            .Include(rr => rr.User)
            .ToListAsync();

        var reviewsByCook = reviews
            .GroupBy(rr => rr.CookInstanceId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Determine which cook instances triggered a promotion (PromotedFrom is set on versions 2+).
        var promotedCookIds = await _db.RecipeVersions
            .Where(rv => rv.RecipeId == recipeId && rv.PromotedFrom != null)
            .Select(rv => rv.PromotedFrom!.Value)
            .ToHashSetAsync();

        // Determine the original recipe date:
        // Use the created_at of the first recipe_version with PromotedFrom = null, if one exists.
        // Otherwise fall back to recipe.created_at.
        var originalVersionDate = await _db.RecipeVersions
            .AsNoTracking()
            .Where(rv => rv.RecipeId == recipeId && rv.PromotedFrom == null)
            .OrderBy(rv => rv.CreatedAt)
            .Select(rv => (DateTime?)rv.CreatedAt)
            .FirstOrDefaultAsync();

        var originalRecipeDate = originalVersionDate.HasValue
            ? new DateTimeOffset(originalVersionDate.Value, TimeSpan.Zero)
            : new DateTimeOffset(recipe.CreatedAt, TimeSpan.Zero);

        var cookInstanceDtos = cookInstances.Select(ci => new CookInstanceSummaryDto
        {
            Id = ci.Id,
            UserId = ci.UserId,
            UserName = ci.User.Name,
            StartedAt = ci.StartedAt,
            CompletedAt = ci.CompletedAt,
            Portions = ci.Portions,
            Notes = ci.Notes,
            WasPromoted = promotedCookIds.Contains(ci.Id),
            Reviews = reviewsByCook.TryGetValue(ci.Id, out var cookReviews)
                ? cookReviews.Select(rr => new CookInstanceReviewSummaryDto
                {
                    UserId = rr.UserId,
                    UserName = rr.User.Name,
                    Rating = rr.Rating,
                    Notes = rr.Notes
                }).ToList()
                : []
        }).ToList();

        return new CookHistoryResponseDto
        {
            CookInstances = cookInstanceDtos,
            OriginalRecipeDate = originalRecipeDate,
            HasOriginalSnapshot = originalVersionDate.HasValue
        };
    }

    // -----------------------------------------------------------------------
    // RESTORE ORIGINAL RECIPE
    // -----------------------------------------------------------------------

    /// <summary>
    /// Restores the recipe's ingredient list from the first snapshot taken before
    /// any promotion (PromotedFrom = null). Returns null + error string if no such
    /// snapshot exists, or if the recipe does not exist.
    /// </summary>
    public async Task<(RestoreResultDto? Result, string? Error, bool NotFound)> RestoreOriginalAsync(
        int recipeId)
    {
        var recipeExists = await _db.Recipes.AnyAsync(r => r.Id == recipeId);
        if (!recipeExists)
            return (null, null, true); // 404

        var originalVersion = await _db.RecipeVersions
            .AsNoTracking()
            .Where(rv => rv.RecipeId == recipeId && rv.PromotedFrom == null)
            .OrderBy(rv => rv.VersionNumber)
            .FirstOrDefaultAsync();

        if (originalVersion == null)
            return (null, "No original snapshot found — recipe has not been promoted yet.", false); // 400

        // Deserialise snapshot JSON back to ingredient rows
        var snapshotItems = JsonSerializer.Deserialize<List<SnapshotIngredientItem>>(
            originalVersion.Snapshot,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (snapshotItems == null || snapshotItems.Count == 0)
            return (null, "Original snapshot is empty or unreadable.", false); // 400

        var restoredIngredients = snapshotItems.Select(item => new RecipeIngredient
        {
            RecipeId = recipeId,
            StageId = item.StageId,
            IngredientId = item.IngredientId,
            Amount = item.Amount,
            UnitId = item.UnitId,
            Notes = item.Notes,
            SortOrder = item.SortOrder,
            WeightGrams = item.WeightGrams
        }).ToList();

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var toDelete = await _db.RecipeIngredients
                .Where(ri => ri.RecipeId == recipeId)
                .ToListAsync();

            _db.RecipeIngredients.RemoveRange(toDelete);
            _db.RecipeIngredients.AddRange(restoredIngredients);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return (new RestoreResultDto
            {
                RecipeId = recipeId,
                RestoredAt = DateTime.UtcNow
            }, null, false);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Restore original failed for recipe {RecipeId}", recipeId);
            throw;
        }
    }

    // -----------------------------------------------------------------------
    // PROMOTE TO RECIPE
    // -----------------------------------------------------------------------

    /// <summary>
    /// Atomically snapshots the current recipe_ingredients into recipe_versions and
    /// overwrites recipe_ingredients with the actuals from the given cook instance.
    /// Returns null + error string for 400/404 conditions.
    /// Throws on unexpected DB errors (caller maps to 500).
    /// </summary>
    public async Task<(PromoteResultDto? Result, string? Error, bool NotFound)> PromoteCookAsync(
        int cookInstanceId, int userId)
    {
        // Load cook instance (ignore soft-delete filter manually so we can distinguish 404 vs 400)
        var cook = await _db.CookInstances
            .IgnoreQueryFilters()
            .Include(ci => ci.Ingredients)
            .FirstOrDefaultAsync(ci => ci.Id == cookInstanceId);

        if (cook == null || cook.DeletedAt != null)
            return (null, null, true); // 404

        if (cook.CompletedAt == null)
            return (null, "Cannot promote an in-progress cook.", false); // 400

        // Load current recipe_ingredients to snapshot
        var currentIngredients = await _db.RecipeIngredients
            .AsNoTracking()
            .Where(ri => ri.RecipeId == cook.RecipeId)
            .OrderBy(ri => ri.SortOrder)
            .ToListAsync();

        // Snapshot serialisation — anonymous objects matching the JSONB structure
        var snapshotObjects = currentIngredients.Select(ri => new
        {
            id = ri.Id,
            recipeId = ri.RecipeId,
            stageId = ri.StageId,
            ingredientId = ri.IngredientId,
            amount = ri.Amount,
            unitId = ri.UnitId,
            notes = ri.Notes,
            sortOrder = ri.SortOrder,
            weightGrams = ri.WeightGrams
        }).ToList();

        var snapshotJson = JsonSerializer.Serialize(snapshotObjects);

        // Determine next version number
        var maxVersion = await _db.RecipeVersions
            .Where(rv => rv.RecipeId == cook.RecipeId)
            .MaxAsync(rv => (int?)rv.VersionNumber) ?? 0;

        var newVersionNumber = maxVersion + 1;

        // Build a lookup of ingredient_id → (stage_id, sort_order) from the original recipe
        // so we can carry those across to the overwritten rows.
        var originalLookup = currentIngredients
            .GroupBy(ri => ri.IngredientId)
            .ToDictionary(g => g.Key, g => g.First()); // first occurrence wins

        // Determine max sort_order from original for appending new ingredients
        var maxSortOrder = currentIngredients.Count > 0
            ? currentIngredients.Max(ri => ri.SortOrder)
            : 0;

        // Build new recipe_ingredient rows from cook_instance_ingredients
        var newIngredients = new List<RecipeIngredient>();
        var appendSortOrder = maxSortOrder;

        foreach (var cii in cook.Ingredients)
        {
            var amountStr = cii.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture);

            if (originalLookup.TryGetValue(cii.IngredientId, out var original))
            {
                newIngredients.Add(new RecipeIngredient
                {
                    RecipeId = cook.RecipeId,
                    StageId = original.StageId,
                    IngredientId = cii.IngredientId,
                    Amount = amountStr,
                    UnitId = cii.UnitId,
                    Notes = cii.Notes,
                    SortOrder = original.SortOrder
                });
            }
            else
            {
                // New ingredient not in original recipe — append
                appendSortOrder += 1;
                newIngredients.Add(new RecipeIngredient
                {
                    RecipeId = cook.RecipeId,
                    StageId = null,
                    IngredientId = cii.IngredientId,
                    Amount = amountStr,
                    UnitId = cii.UnitId,
                    Notes = cii.Notes,
                    SortOrder = appendSortOrder
                });
            }
        }

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            // Write 1: snapshot
            var version = new RecipeVersion
            {
                RecipeId = cook.RecipeId,
                VersionNumber = newVersionNumber,
                Snapshot = snapshotJson,
                // First promotion snapshots the original recipe — not promoted from any cook.
                // Subsequent promotions snapshot a state that was itself installed by a cook.
                PromotedFrom = maxVersion == 0 ? null : cookInstanceId,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };
            _db.RecipeVersions.Add(version);

            // Write 2: overwrite ingredients
            // Delete all existing recipe_ingredients for this recipe
            var toDelete = await _db.RecipeIngredients
                .Where(ri => ri.RecipeId == cook.RecipeId)
                .ToListAsync();

            _db.RecipeIngredients.RemoveRange(toDelete);

            // Add the new rows
            _db.RecipeIngredients.AddRange(newIngredients);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return (new PromoteResultDto
            {
                VersionId = version.Id,
                VersionNumber = version.VersionNumber,
                RecipeId = cook.RecipeId,
                PromotedAt = version.CreatedAt
            }, null, false);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Promote failed for cook instance {CookInstanceId}", cookInstanceId);
            throw;
        }
    }

    // -----------------------------------------------------------------------
    // VERSION HISTORY
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns all recipe_versions for the given recipe, ordered by version_number DESC.
    /// Returns an empty list if no versions exist. Returns null if the recipe does not exist.
    /// </summary>
    public async Task<List<RecipeVersionSummaryDto>?> GetVersionsByRecipeAsync(int recipeId)
    {
        // Verify recipe exists (query filter excludes soft-deleted)
        var recipeExists = await _db.Recipes.AnyAsync(r => r.Id == recipeId);
        if (!recipeExists)
            return null;

        var versions = await _db.RecipeVersions
            .AsNoTracking()
            .Where(rv => rv.RecipeId == recipeId)
            .Include(rv => rv.CreatedByUser)
            .Include(rv => rv.PromotedFromCookInstance)
            .OrderByDescending(rv => rv.VersionNumber)
            .ToListAsync();

        return versions.Select(rv => new RecipeVersionSummaryDto
        {
            Id = rv.Id,
            VersionNumber = rv.VersionNumber,
            CreatedAt = rv.CreatedAt,
            CreatedByName = rv.CreatedByUser?.Name,
            PromotedFromCookDate = rv.PromotedFromCookInstance != null
                ? DateOnly.FromDateTime(rv.PromotedFromCookInstance.StartedAt)
                : null
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

        // Load reviews for this cook instance
        var ciReviews = await _db.RecipeReviews
            .AsNoTracking()
            .Where(rr => rr.CookInstanceId == id)
            .Include(rr => rr.User)
            .ToListAsync();

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
            StageGroups = grouped,
            Reviews = ciReviews.Select(rr => new CookInstanceReviewSummaryDto
            {
                UserId = rr.UserId,
                UserName = rr.User.Name,
                Rating = rr.Rating,
                Notes = rr.Notes
            }).ToList()
        };
    }

    // -----------------------------------------------------------------------
    // Private records
    // -----------------------------------------------------------------------

    /// <summary>
    /// Mirrors the anonymous object structure written into recipe_versions.Snapshot
    /// by PromoteCookAsync. Used to deserialise snapshot JSON when restoring the original.
    /// </summary>
    private sealed record SnapshotIngredientItem(
        int Id,
        int RecipeId,
        int? StageId,
        int IngredientId,
        string? Amount,
        int? UnitId,
        string? Notes,
        int SortOrder,
        decimal? WeightGrams
    );
}
