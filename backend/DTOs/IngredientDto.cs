namespace WalkerFcb.Api.DTOs;

/// <summary>
/// Read-only DTO returned by GET /api/ingredients.
/// </summary>
public class IngredientDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

/// <summary>
/// Request body for POST /api/ingredients.
/// </summary>
public class CreateIngredientDto
{
    public string Name { get; init; } = string.Empty;
}
