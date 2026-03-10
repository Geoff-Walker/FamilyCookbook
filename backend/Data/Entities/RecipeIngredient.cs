namespace WalkerFcb.Api.Data.Entities;

/// <summary>
/// Joins an <see cref="Ingredient"/> to a <see cref="Recipe"/>, optionally scoped to
/// a specific <see cref="RecipeStage"/>. <see cref="Amount"/> is TEXT for flexibility
/// (e.g. "a pinch", "2–3"). <see cref="Unit"/> is nullable.
/// Fluent API configuration is in <see cref="WalkerDbContext"/>.
/// </summary>
public class RecipeIngredient
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public int? StageId { get; set; }
    public int IngredientId { get; set; }
    public string? Amount { get; set; }
    public int? UnitId { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }

    public Recipe Recipe { get; set; } = null!;
    public RecipeStage? Stage { get; set; }
    public Ingredient Ingredient { get; set; } = null!;
    public Unit? Unit { get; set; }
}
