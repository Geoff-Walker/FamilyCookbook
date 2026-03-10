namespace WalkerFcb.Api.Data.Entities;

/// <summary>
/// Many-to-many join table between <see cref="Recipe"/> and <see cref="Tag"/>.
/// Composite PK <c>(RecipeId, TagId)</c> — no surrogate key.
/// Fluent API configuration is in <see cref="WalkerDbContext"/>.
/// </summary>
public class RecipeTag
{
    public int RecipeId { get; set; }
    public int TagId { get; set; }

    public Recipe Recipe { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
