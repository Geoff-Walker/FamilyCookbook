namespace WalkerFcb.Api.DTOs;

// ---------------------------------------------------------------------------
// Shared response DTO
// ---------------------------------------------------------------------------

/// <summary>
/// Response DTO for a single meal plan slot.
/// Returned by GET (list) and POST (create).
/// <c>RecipeName</c> is populated via JOIN to recipes when <c>SlotType</c> is 'recipe'.
/// </summary>
public class MealPlanSlotDto
{
    public int Id { get; set; }
    public DateOnly SlotDate { get; set; }
    public string SlotType { get; set; } = string.Empty;
    public int? RecipeId { get; set; }
    public string? RecipeName { get; set; }
    public int BatchMultiplier { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
}

// ---------------------------------------------------------------------------
// POST /api/meal-plan-slots — request
// ---------------------------------------------------------------------------

/// <summary>
/// Request body for creating a new meal plan slot.
/// Validation is enforced at the API layer — see <c>MealPlanSlotService</c>.
/// </summary>
public class CreateMealPlanSlotDto
{
    public DateOnly SlotDate { get; set; }
    public string SlotType { get; set; } = string.Empty;
    public int? RecipeId { get; set; }
    public int BatchMultiplier { get; set; } = 1;
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
}

// ---------------------------------------------------------------------------
// PATCH /api/meal-plan-slots/{id} — request
// ---------------------------------------------------------------------------

/// <summary>
/// Partial update for a meal plan slot. All fields are optional —
/// only non-null values are applied.
/// </summary>
public class PatchMealPlanSlotDto
{
    public DateOnly? SlotDate { get; set; }
    public string? SlotType { get; set; }
    public int? RecipeId { get; set; }
    public int? BatchMultiplier { get; set; }
    public string? Notes { get; set; }
    public int? SortOrder { get; set; }
}
