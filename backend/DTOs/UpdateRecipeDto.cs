namespace WalkerFcb.Api.DTOs;

/// <summary>
/// Request body for PUT /api/recipes/:id.
/// Full replacement — child collections (stages, steps, ingredients, tags) are replaced wholesale.
/// <see cref="Title"/> is required.
/// </summary>
public class UpdateRecipeDto
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Source { get; init; }
    /// <summary>
    /// External image URL for the recipe hero image.
    /// TODO (next sprint): replace with multipart file upload endpoint — this field
    /// will accept the resolved URL returned after the upload is stored.
    /// </summary>
    public string? ImageUrl { get; init; }
    public int? PrepTimeMinutes { get; init; }
    public int? CookTimeMinutes { get; init; }
    public int? Servings { get; init; }
    public List<int> TagIds { get; init; } = [];
    public List<UpdateRecipeStageDto> Stages { get; init; } = [];
}

/// <summary>
/// A stage within an <see cref="UpdateRecipeDto"/>.
/// </summary>
public class UpdateRecipeStageDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public List<UpdateRecipeStepDto> Steps { get; init; } = [];
    public List<UpdateRecipeIngredientDto> Ingredients { get; init; } = [];
}

/// <summary>
/// A step within an <see cref="UpdateRecipeStageDto"/>.
/// </summary>
public class UpdateRecipeStepDto
{
    public string Instruction { get; init; } = string.Empty;
}

/// <summary>
/// An ingredient line within an <see cref="UpdateRecipeStageDto"/>.
/// </summary>
public class UpdateRecipeIngredientDto
{
    public string IngredientName { get; init; } = string.Empty;
    public string? Amount { get; init; }
    public int? UnitId { get; init; }
    public string? Notes { get; init; }
    public decimal? WeightGrams { get; init; }
}
