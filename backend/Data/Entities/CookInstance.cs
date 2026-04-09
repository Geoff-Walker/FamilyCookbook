namespace WalkerFcb.Api.Data.Entities;

/// <summary>
/// Records a single cooking session for a recipe. Soft-deleted via <see cref="DeletedAt"/>;
/// never hard-deleted. <see cref="CompletedAt"/> is NULL while the cook is in progress.
/// Fluent API configuration is in <see cref="WalkerDbContext"/>.
/// </summary>
public class CookInstance
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public int UserId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? Portions { get; set; }
    public string? Notes { get; set; }

    public Recipe Recipe { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<CookInstanceIngredient> Ingredients { get; set; } = [];
}
