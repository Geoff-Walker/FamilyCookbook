namespace WalkerFcb.Api.Data.Entities;

/// <summary>
/// A measurement unit (e.g. "gram", "ml", "tbsp"). Grouped by <see cref="UnitType"/>
/// (e.g. "weight", "volume", "count"). FK from <see cref="RecipeIngredient"/> is SET NULL.
/// Fluent API configuration is in <see cref="WalkerDbContext"/>.
/// </summary>
public class Unit
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Abbreviation { get; set; }
    public string UnitType { get; set; } = string.Empty;

    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = [];
}
