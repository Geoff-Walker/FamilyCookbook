namespace WalkerFcb.Api.Data.Entities;

/// <summary>
/// A family member's review of a recipe. <see cref="Rating"/> is constrained 0–5
/// (in 0.5 increments) by a CHECK constraint in the migration. <see cref="MadeOn"/>
/// records when they cooked it. <see cref="CookInstanceId"/> links the review to the
/// specific cook session it was submitted from (null for reviews created outside of
/// the complete-cook flow). Fluent API configuration is in <see cref="WalkerDbContext"/>.
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
    /// <summary>
    /// The cook instance this review was submitted from. Null for reviews
    /// created outside the complete-cook flow.
    /// </summary>
    public int? CookInstanceId { get; set; }

    public Recipe Recipe { get; set; } = null!;
    public User User { get; set; } = null!;
    public CookInstance? CookInstance { get; set; }
}
