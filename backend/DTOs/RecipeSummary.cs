namespace WalkerFcb.Api.DTOs;

/// <summary>
/// Lightweight projection used for recipe list views.
/// Returned by <c>IRecipeRepository.GetAllAsync()</c> — contains only the fields
/// needed to render a recipe card, not the full recipe detail.
/// </summary>
public class RecipeSummary
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int? PrepTimeMinutes { get; init; }
    public int? CookTimeMinutes { get; init; }
    public int? Servings { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? ImageUrl { get; init; }

    /// <summary>Average rating from all reviews, or null if there are no reviews.</summary>
    public double? AverageRating { get; init; }

    /// <summary>Total number of reviews.</summary>
    public int ReviewCount { get; init; }

    /// <summary>Tags attached to this recipe, ordered by category then tag name.</summary>
    public List<RecipeTagSummary> Tags { get; init; } = [];
}

/// <summary>
/// Tag information included in a <see cref="RecipeSummary"/>.
/// </summary>
public class RecipeTagSummary
{
    public int TagId { get; init; }
    public string TagName { get; init; } = string.Empty;
    public string TagSlug { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
}
