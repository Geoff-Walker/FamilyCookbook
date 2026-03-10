namespace WalkerFcb.Api.Data.Entities;

/// <summary>
/// A canonical ingredient shared across recipes (e.g. "butter", "plain flour").
/// The <c>ingredients.name</c> column has a case-insensitive unique index enforced
/// via raw SQL in the migration. FK from <see cref="RecipeIngredient"/> is RESTRICT.
/// Fluent API configuration is in <see cref="WalkerDbContext"/>.
/// </summary>
public class Ingredient
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = [];
}
