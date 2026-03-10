namespace WalkerFcb.Api.Data.Entities;

/// <summary>
/// Minimal stub — full entity definition in WAL-27.
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
