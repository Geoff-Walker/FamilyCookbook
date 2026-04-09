namespace WalkerFcb.Api.Data.Entities;

/// <summary>
/// A family member's review of a recipe. <see cref="Rating"/> is constrained 1–5
/// by a CHECK constraint in the migration. <see cref="MadeOn"/> records when they
/// cooked it. Fluent API configuration is in <see cref="WalkerDbContext"/>.
/// </summary>
public class RecipeReview
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public int UserId { get; set; }
    public decimal Rating { get; set; }
    public string? Notes { get; set; }
    public DateOnly? MadeOn { get; set; }
    public DateTime CreatedAt { get; set; }

    public Recipe Recipe { get; set; } = null!;
    public User User { get; set; } = null!;
}
