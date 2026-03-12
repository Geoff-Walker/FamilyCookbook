namespace WalkerFcb.Api.DTOs;

/// <summary>
/// API-layer summary DTO returned by GET /api/recipes.
/// Contains everything needed to render a recipe card including tags and per-user ratings.
/// </summary>
public class RecipeSummaryDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public int? PrepTimeMinutes { get; init; }
    public int? CookTimeMinutes { get; init; }
    public string? ImageUrl { get; init; }
    public List<RecipeSummaryTagDto> Tags { get; init; } = [];
    public List<RecipeSummaryRatingDto> Ratings { get; init; } = [];
}

/// <summary>
/// Tag item within a <see cref="RecipeSummaryDto"/>.
/// </summary>
public class RecipeSummaryTagDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
}

/// <summary>
/// Per-user average rating within a <see cref="RecipeSummaryDto"/>.
/// Each entry represents one user who has reviewed the recipe.
/// </summary>
public class RecipeSummaryRatingDto
{
    public int UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public double AverageRating { get; init; }
}
