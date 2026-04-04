namespace WalkerFcb.Api.DTOs;

/// <summary>
/// Request body for POST /api/recipes/{id}/image/idealise.
/// </summary>
public class IdealiseImageRequestDto
{
    /// <summary>
    /// Visual style to apply. Required.
    /// Must be one of: rustic, minimalist, mediterranean, cosy, classic, moody.
    /// </summary>
    public string Style { get; init; } = string.Empty;

    /// <summary>
    /// Optional free-text addendum appended verbatim to the assembled prompt.
    /// </summary>
    public string? FreeText { get; init; }
}

/// <summary>
/// Response body for POST /api/recipes/{id}/image/idealise.
/// </summary>
public class IdealiseImageResponseDto
{
    /// <summary>
    /// The resolved public URL of the newly idealised and stored image.
    /// </summary>
    public string ImageUrl { get; init; } = string.Empty;
}
