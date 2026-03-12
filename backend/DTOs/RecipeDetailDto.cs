namespace WalkerFcb.Api.DTOs;

/// <summary>
/// Full recipe detail returned by GET /api/recipes/:id, POST /api/recipes, and PUT /api/recipes/:id.
/// Contains all stages, steps, ingredients, tags, and reviews.
/// </summary>
public class RecipeDetailDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Source { get; init; }
    public string? ImageUrl { get; init; }
    public int? PrepTimeMinutes { get; init; }
    public int? CookTimeMinutes { get; init; }
    public int? Servings { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public List<RecipeDetailStageDto> Stages { get; init; } = [];
    public List<RecipeDetailTagDto> Tags { get; init; } = [];
    public List<RecipeDetailReviewDto> Reviews { get; init; } = [];
}

/// <summary>
/// A stage within a <see cref="RecipeDetailDto"/>, ordered by sort_order.
/// </summary>
public class RecipeDetailStageDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int SortOrder { get; init; }
    public List<RecipeDetailStepDto> Steps { get; init; } = [];
    public List<RecipeDetailIngredientDto> Ingredients { get; init; } = [];
}

/// <summary>
/// A step within a <see cref="RecipeDetailStageDto"/>.
/// </summary>
public class RecipeDetailStepDto
{
    public int Id { get; init; }
    public string Instruction { get; init; } = string.Empty;
    public int SortOrder { get; init; }
}

/// <summary>
/// An ingredient line within a <see cref="RecipeDetailStageDto"/>.
/// </summary>
public class RecipeDetailIngredientDto
{
    public int Id { get; init; }
    public int IngredientId { get; init; }
    public string IngredientName { get; init; } = string.Empty;
    public string? Amount { get; init; }
    public int? UnitId { get; init; }
    public string? UnitName { get; init; }
    public string? UnitAbbreviation { get; init; }
    public string? Notes { get; init; }
    public int SortOrder { get; init; }
}

/// <summary>
/// Tag item within a <see cref="RecipeDetailDto"/>.
/// </summary>
public class RecipeDetailTagDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
}

/// <summary>
/// Review entry within a <see cref="RecipeDetailDto"/>.
/// </summary>
public class RecipeDetailReviewDto
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public int Rating { get; init; }
    public string? Notes { get; init; }
    public DateOnly? MadeOn { get; init; }
    public DateTime CreatedAt { get; init; }
}
