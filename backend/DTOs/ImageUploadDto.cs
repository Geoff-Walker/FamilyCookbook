namespace WalkerFcb.Api.DTOs;

/// <summary>
/// Response body for POST /api/recipes/{id}/image.
/// </summary>
public class ImageUploadResponseDto
{
    /// <summary>
    /// The resolved public URL for the uploaded image.
    /// Served by nginx from the /uploads/ path prefix.
    /// </summary>
    public string ImageUrl { get; init; } = string.Empty;
}
