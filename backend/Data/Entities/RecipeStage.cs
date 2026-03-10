namespace WalkerFcb.Api.Data.Entities;

/// <summary>
/// A named stage within a recipe (e.g. "Make the pastry", "Prepare the filling").
/// <see cref="SubRecipeId"/> optionally references another recipe whose stages are
/// substituted for this stage's steps; SET NULL on delete.
/// Fluent API configuration is in <see cref="WalkerDbContext"/>.
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
