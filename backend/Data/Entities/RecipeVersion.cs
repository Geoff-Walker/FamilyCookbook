namespace WalkerFcb.Api.Data.Entities;

/// <summary>
/// Snapshot of a recipe's ingredient state taken before a promote-to-recipe action.
/// <see cref="Snapshot"/> is stored as JSONB and contains the full recipe_ingredients
/// state at the time of the snapshot. <see cref="PromotedFrom"/> identifies the cook
/// instance that triggered the snapshot; NULL indicates manual or future-use cases.
/// Fluent API configuration is in <see cref="WalkerDbContext"/>.
/// </summary>
public class RecipeVersion
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public int VersionNumber { get; set; }
    /// <summary>
    /// Full recipe_ingredients state at time of snapshot, stored as JSONB.
    /// Serialised/deserialised by the API layer.
    /// </summary>
    public string Snapshot { get; set; } = string.Empty;
    public int? PromotedFrom { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }

    public Recipe Recipe { get; set; } = null!;
    public CookInstance? PromotedFromCookInstance { get; set; }
    public User? CreatedByUser { get; set; }
}
