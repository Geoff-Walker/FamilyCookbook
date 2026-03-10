namespace WalkerFcb.Api.Data.Entities;

/// <summary>
/// A grouping for tags (e.g. "Cuisine", "Meal Type", "Dietary").
/// <c>name</c> has a unique index. Fluent API configuration is in <see cref="WalkerDbContext"/>.
/// </summary>
public class TagCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Tag> Tags { get; set; } = [];
}
