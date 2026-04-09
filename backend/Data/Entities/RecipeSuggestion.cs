namespace WalkerFcb.Api.Data.Entities;

/// <summary>
/// Represents a recipe suggestion submitted by a family member for The Geoff Filter.
/// Either <see cref="SuggestionUrl"/> or <see cref="SuggestionText"/> must be set —
/// this constraint is enforced at the API layer, not at DB level.
/// Lifecycle: pending → accepted / deleted / backlogged.
/// When accepted, <see cref="RecipeId"/> is populated to link to the created recipe.
/// Fluent API configuration is in <see cref="WalkerDbContext"/>.
/// </summary>
public class RecipeSuggestion
{
    public int Id { get; set; }
    public int SuggestedBy { get; set; }
    public string? SuggestionUrl { get; set; }
    public string? SuggestionText { get; set; }
    /// <summary>
    /// Valid values: 'pending', 'accepted', 'deleted', 'backlogged'.
    /// Enforced at API layer.
    /// </summary>
    public string Status { get; set; } = "pending";
    /// <summary>
    /// Populated when <see cref="Status"/> is 'accepted'. Links to the recipe
    /// created from this suggestion via the Geoff Filter accept flow.
    /// </summary>
    public int? RecipeId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User SuggestedByUser { get; set; } = null!;
    public Recipe? Recipe { get; set; }
}
