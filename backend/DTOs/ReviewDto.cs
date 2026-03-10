namespace WalkerFcb.Api.DTOs;

/// <summary>
/// A single review entry returned by GET /api/recipes/:recipeId/reviews
/// and POST /api/recipes/:recipeId/reviews.
/// Dates are serialised as ISO 8601 strings by the JSON serialiser.
/// </summary>
public class ReviewDto
{
    public int Id { get; init; }
    public int RecipeId { get; init; }
    public int UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public int Rating { get; init; }
    public string? Notes { get; init; }
    public DateOnly? MadeOn { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Request body accepted by POST /api/recipes/:recipeId/reviews.
/// </summary>
public class CreateReviewDto
{
    public int UserId { get; init; }
    public int Rating { get; init; }
    public string? Notes { get; init; }
    public string? MadeOn { get; init; }
}
