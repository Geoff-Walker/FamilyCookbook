namespace WalkerFcb.Api.Data.Entities;

/// <summary>
/// Represents a family member who can leave reviews on recipes.
/// Fluent API configuration is in <see cref="WalkerDbContext"/>.
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<RecipeReview> Reviews { get; set; } = [];
}
