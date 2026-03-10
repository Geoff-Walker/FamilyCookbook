namespace WalkerFcb.Api.Data.Entities;

/// <summary>
/// Minimal stub — full entity definition in WAL-27.
/// </summary>
public class RecipeStage
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public int? SubRecipeId { get; set; }

    public Recipe Recipe { get; set; } = null!;
    public Recipe? SubRecipe { get; set; }
    public ICollection<RecipeStep> Steps { get; set; } = [];
    public ICollection<RecipeIngredient> Ingredients { get; set; } = [];
}
