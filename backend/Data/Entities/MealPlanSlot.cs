namespace WalkerFcb.Api.Data.Entities;

/// <summary>
/// Represents a single planned meal for a given date in the Meal Planner.
/// Slots are keyed directly by date — there is no parent plan record.
/// <see cref="SlotType"/> determines what the slot contains:
///   'recipe'      — a linked recipe (<see cref="RecipeId"/> is populated)
///   'if_its'      — a free-text note (<see cref="Notes"/> is used)
///   'not_defined' — a placeholder with nothing assigned
/// Valid slot types are enforced at the API layer, not at DB level.
/// Fluent API configuration is in <see cref="WalkerDbContext"/>.
/// </summary>
public class MealPlanSlot
{
    public int Id { get; set; }
    public DateOnly SlotDate { get; set; }
    public string SlotType { get; set; } = string.Empty;
    /// <summary>
    /// Populated when <see cref="SlotType"/> is 'recipe'. NULL otherwise.
    /// Deleting a recipe does not cascade-delete the slot.
    /// </summary>
    public int? RecipeId { get; set; }
    /// <summary>
    /// Scales ingredient quantities for shopping list generation. Minimum 1, enforced at API layer.
    /// </summary>
    public int BatchMultiplier { get; set; }
    /// <summary>
    /// Free-text note, primarily used when <see cref="SlotType"/> is 'if_its'.
    /// </summary>
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }

    public Recipe? Recipe { get; set; }
}
