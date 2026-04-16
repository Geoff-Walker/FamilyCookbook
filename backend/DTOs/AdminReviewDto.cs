namespace WalkerFcb.Api.DTOs;

/// <summary>
/// A flat review entry returned by the admin endpoints (GET /api/reviews).
/// Includes recipe title and user name so the admin page can display the
/// full context without the client needing to join data itself.
/// </summary>
public class AdminReviewDto
{
    public int Id { get; init; }
    public int RecipeId { get; init; }
    public string RecipeTitle { get; init; } = string.Empty;
    public int UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public decimal Rating { get; init; }
    public string? Notes { get; init; }
    public DateOnly? MadeOn { get; init; }
    public DateTime CreatedAt { get; init; }
}
