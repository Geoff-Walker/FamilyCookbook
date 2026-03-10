namespace WalkerFcb.Api.Data.Entities;

/// <summary>
/// A recipe tag belonging to a <see cref="TagCategory"/> (e.g. "Italian" in "Cuisine").
/// <see cref="Slug"/> is URL-safe and unique. Fluent API configuration is in <see cref="WalkerDbContext"/>.
/// </summary>
public class Tag
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    public TagCategory Category { get; set; } = null!;
    public ICollection<RecipeTag> RecipeTags { get; set; } = [];
}
