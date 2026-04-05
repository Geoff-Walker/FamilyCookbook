namespace WalkerFcb.Api.DTOs;

/// <summary>
/// Request body for POST /api/recipes.
/// <see cref="Title"/> is required. All other fields are optional.
/// </summary>
public class CreateRecipeDto
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
    public List<CreateRecipeStageDto> Stages { get; init; } = [];
}

/// <summary>
/// A stage within a <see cref="CreateRecipeDto"/>.
/// </summary>
public class CreateRecipeStageDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public List<CreateRecipeStepDto> Steps { get; init; } = [];
    public List<CreateRecipeIngredientDto> Ingredients { get; init; } = [];
}

/// <summary>
/// A step within a <see cref="CreateRecipeStageDto"/>.
/// </summary>
public class CreateRecipeStepDto
{
    public string Instruction { get; init; } = string.Empty;
}

/// <summary>
/// An ingredient line within a <see cref="CreateRecipeStageDto"/>.
/// Ingredient is identified by name (upserted if not found).
/// </summary>
public class CreateRecipeIngredientDto
{
    public string IngredientName { get; init; } = string.Empty;
    public string? Amount { get; init; }
    public int? UnitId { get; init; }
    public string? Notes { get; init; }
    public decimal? WeightGrams { get; init; }
}
