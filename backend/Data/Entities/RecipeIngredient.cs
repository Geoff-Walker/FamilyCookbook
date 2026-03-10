namespace WalkerFcb.Api.Data.Entities;

/// <summary>
/// Minimal stub — full entity definition in WAL-27.
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
