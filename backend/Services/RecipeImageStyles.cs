namespace WalkerFcb.Api.Services;

/// <summary>
/// Shared style map and valid-style list used by both <see cref="ImageGenerationService"/>
/// (WAL-34, text-to-image) and <see cref="ImageIdealiseService"/> (WAL-35, img2img).
/// A single source of truth prevents duplication.
/// </summary>
public static class RecipeImageStyles
{
    /// <summary>
    /// Maps each valid style name to its prompt clause.
    /// Comparison is case-insensitive.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> StyleMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["rustic"]        = "warm natural light, wooden surfaces, linen, homestyle presentation",
            ["minimalist"]    = "clean white background, precise plating, contemporary food photography",
            ["mediterranean"] = "bright warm light, terracotta, olive wood, sunshine colours",
            ["cosy"]          = "golden hour light, rich warm tones, seasonal garnish, soft shadows",
            ["classic"]       = "neutral background, traditional food styling, garnished, cookbook standard",
            ["moody"]         = "dramatic low-key lighting, dark slate surface, restaurant plating",
        };

    /// <summary>
    /// Ordered list of valid style names, suitable for error messages.
    /// </summary>
    public static readonly IReadOnlyCollection<string> ValidStyles = StyleMap.Keys.ToList();

    /// <summary>
    /// Returns the prompt clause for <paramref name="style"/>, or <c>null</c> if unknown.
    /// </summary>
    public static string? GetClause(string style) =>
        StyleMap.TryGetValue(style, out var clause) ? clause : null;
}
