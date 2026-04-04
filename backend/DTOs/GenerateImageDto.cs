namespace WalkerFcb.Api.DTOs;

/// <summary>
/// Request body for POST /api/recipes/{id}/image/generate.
/// </summary>
public class GenerateImageRequestDto
{
    /// <summary>
    /// Short description of the dish. Required.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Key ingredients to include in the prompt. Required; at least one entry.
    /// </summary>
    public List<string> Ingredients { get; init; } = [];

    /// <summary>
    /// Visual style. Must be one of: rustic, minimalist, mediterranean, cosy, classic, moody.
    /// </summary>
    public string Style { get; init; } = string.Empty;

    /// <summary>
    /// Optional free-text addendum appended verbatim to the assembled prompt.
    /// </summary>
    public string? FreeText { get; init; }
}

/// <summary>
/// Response body for POST /api/recipes/{id}/image/generate.
/// </summary>
public class GenerateImageResponseDto
{
    /// <summary>
    /// The resolved public URL of the newly generated and stored image.
    /// </summary>
    public string ImageUrl { get; init; } = string.Empty;
}
