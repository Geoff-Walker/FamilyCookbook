using Microsoft.EntityFrameworkCore;
using WalkerFcb.Api.Data;
using WalkerFcb.Api.Data.Entities;
using WalkerFcb.Api.DTOs;

namespace WalkerFcb.Api.Services;

/// <summary>
/// Business logic for meal plan slot CRUD.
/// Slots are ephemeral operational data — hard delete only, no soft delete.
/// </summary>
public class MealPlanSlotService
{
    private static readonly HashSet<string> ValidSlotTypes =
        new(StringComparer.Ordinal) { "recipe", "if_its", "not_defined" };

    private readonly WalkerDbContext _db;

    public MealPlanSlotService(WalkerDbContext db)
    {
        _db = db;
    }

    // -----------------------------------------------------------------------
    // GET — date range query
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns all meal plan slots where slot_date is within [from, to] (inclusive),
    /// with recipe name joined from the recipes table.
    /// </summary>
    public async Task<List<MealPlanSlotDto>> GetByDateRangeAsync(DateOnly from, DateOnly to)
    {
        // LEFT JOIN to recipes so non-recipe slots still appear.
        // The recipe soft-delete query filter is active, so deleted recipes
        // will return null for the navigation property — that is intentional.
        var slots = await _db.MealPlanSlots
            .AsNoTracking()
            .Where(m => m.SlotDate >= from && m.SlotDate <= to)
            .Include(m => m.Recipe)
            .OrderBy(m => m.SlotDate)
            .ThenBy(m => m.SortOrder)
            .ToListAsync();

        return slots.Select(ToDto).ToList();
    }

    // -----------------------------------------------------------------------
    // POST — create
    // -----------------------------------------------------------------------

    /// <summary>
    /// Validates and creates a new meal plan slot.
    /// Returns the created slot DTO on success.
    /// Returns a non-null <c>ValidationError</c> string on AC failure.
    /// Returns <c>NotFound = true</c> when the referenced recipe does not exist (404).
    /// </summary>
    public async Task<(MealPlanSlotDto? Dto, string? ValidationError, bool NotFound)> CreateAsync(
        CreateMealPlanSlotDto request)
    {
        // AC 3 — slot_type must be a known value
        if (!ValidSlotTypes.Contains(request.SlotType))
            return (null, $"slotType '{request.SlotType}' is invalid — must be one of: recipe, if_its, not_defined", false);

        // AC 4 — batch_multiplier must be a positive integer >= 1
        if (request.BatchMultiplier < 1)
            return (null, "batchMultiplier must be a positive integer (≥ 1)", false);

        // AC 5 — if slot_type = 'recipe', recipe_id is required and must reference a non-deleted recipe
        if (request.SlotType == "recipe")
        {
            if (!request.RecipeId.HasValue)
                return (null, "recipeId is required when slotType is 'recipe'", false);

            var recipeExists = await _db.Recipes.AnyAsync(r => r.Id == request.RecipeId.Value);
            if (!recipeExists)
                return (null, null, true); // 404 — recipe not found or soft-deleted
        }

        // AC 6 — if slot_type = 'if_its', notes is required
        if (request.SlotType == "if_its" && string.IsNullOrWhiteSpace(request.Notes))
            return (null, "notes is required when slotType is 'if_its'", false);

        var slot = new MealPlanSlot
        {
            SlotDate = request.SlotDate,
            SlotType = request.SlotType,
            RecipeId = request.SlotType == "recipe" ? request.RecipeId : null,
            BatchMultiplier = request.BatchMultiplier,
            Notes = request.Notes,
            SortOrder = request.SortOrder
        };

        _db.MealPlanSlots.Add(slot);
        await _db.SaveChangesAsync();

        // Re-load with Recipe navigation for recipeName in response
        await _db.Entry(slot).Reference(m => m.Recipe).LoadAsync();

        return (ToDto(slot), null, false);
    }

    // -----------------------------------------------------------------------
    // PATCH — partial update
    // -----------------------------------------------------------------------

    /// <summary>
    /// Applies a partial update to an existing meal plan slot.
    /// Returns false if the slot is not found (404).
    /// Returns a non-null <c>ValidationError</c> string on AC failure (400).
    /// </summary>
    public async Task<(bool Found, string? ValidationError)> PatchAsync(int id, PatchMealPlanSlotDto patch)
    {
        var slot = await _db.MealPlanSlots.FindAsync(id);
        if (slot == null)
            return (false, null);

        if (patch.SlotDate.HasValue)
            slot.SlotDate = patch.SlotDate.Value;

        if (patch.SlotType != null)
        {
            if (!ValidSlotTypes.Contains(patch.SlotType))
                return (true, $"slotType '{patch.SlotType}' is invalid — must be one of: recipe, if_its, not_defined");
            slot.SlotType = patch.SlotType;
        }

        if (patch.BatchMultiplier.HasValue)
        {
            if (patch.BatchMultiplier.Value < 1)
                return (true, "batchMultiplier must be a positive integer (≥ 1)");
            slot.BatchMultiplier = patch.BatchMultiplier.Value;
        }

        // Allow explicit null to clear recipeId / notes
        if (patch.RecipeId != null)
            slot.RecipeId = patch.RecipeId;

        if (patch.Notes != null)
            slot.Notes = patch.Notes;

        if (patch.SortOrder.HasValue)
            slot.SortOrder = patch.SortOrder.Value;

        await _db.SaveChangesAsync();
        return (true, null);
    }

    // -----------------------------------------------------------------------
    // DELETE — hard delete
    // -----------------------------------------------------------------------

    /// <summary>
    /// Hard-deletes a meal plan slot. Returns false if not found (404).
    /// </summary>
    public async Task<bool> DeleteAsync(int id)
    {
        var slot = await _db.MealPlanSlots.FindAsync(id);
        if (slot == null)
            return false;

        _db.MealPlanSlots.Remove(slot);
        await _db.SaveChangesAsync();
        return true;
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private static MealPlanSlotDto ToDto(MealPlanSlot slot) => new()
    {
        Id = slot.Id,
        SlotDate = slot.SlotDate,
        SlotType = slot.SlotType,
        RecipeId = slot.RecipeId,
        RecipeName = slot.Recipe?.Title,
        BatchMultiplier = slot.BatchMultiplier,
        Notes = slot.Notes,
        SortOrder = slot.SortOrder
    };
}
