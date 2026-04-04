using Microsoft.EntityFrameworkCore;
using OpenAI;
using OpenAI.Images;
using WalkerFcb.Api.Data;

namespace WalkerFcb.Api.Services;

/// <summary>
/// Generates a recipe hero image via OpenAI gpt-image-1, downloads it, and stores it
/// to the same Docker volume used by <see cref="ImageUploadService"/> (/app/uploads/recipes/{recipeId}/).
/// </summary>
public class ImageGenerationService
{
    private const string ImageModel = "gpt-image-1";
    private const string ImageSize = "1024x1024";
    private const string BaseQuality = "Professional food photography, appetising, high resolution, natural light, shallow depth of field, realistic";

    // Style map and valid-style list live in RecipeImageStyles — shared with ImageIdealiseService.
    public static IReadOnlyCollection<string> ValidStyles => RecipeImageStyles.ValidStyles;

    private readonly WalkerDbContext _db;
    private readonly OpenAIClient? _openAi;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ImageGenerationService> _logger;

    public ImageGenerationService(
        WalkerDbContext db,
        OpenAIClient? openAi,
        HttpClient httpClient,
        ILogger<ImageGenerationService> logger)
    {
        _db = db;
        _openAi = openAi;
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Assembles a prompt from the four layers, calls gpt-image-1, downloads the result,
    /// stores it to the image volume, updates <c>recipes.image_url</c>, and returns the public URL.
    /// </summary>
    public async Task<ImageGenerationResult> GenerateAsync(
        int recipeId,
        string description,
        IEnumerable<string> ingredients,
        string style,
        string? freeText)
    {
        // --- Recipe lookup ---
        var recipe = await _db.Recipes.FirstOrDefaultAsync(r => r.Id == recipeId && r.DeletedAt == null);
        if (recipe is null)
            return ImageGenerationResult.NotFound();

        // --- Validation ---
        if (string.IsNullOrWhiteSpace(description))
            return ImageGenerationResult.Failure("description is required.");

        var ingredientList = ingredients.Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
        if (ingredientList.Count == 0)
            return ImageGenerationResult.Failure("ingredients must contain at least one entry.");

        var styleClause = RecipeImageStyles.GetClause(style);
        if (styleClause is null)
            return ImageGenerationResult.InvalidStyle($"style must be one of: {string.Join(", ", ValidStyles)}.");

        // --- OpenAI client guard ---
        if (_openAi is null)
        {
            _logger.LogWarning("Image generation requested for recipe {Id} but OpenAI client is not configured.", recipeId);
            return ImageGenerationResult.Failure("Image generation is unavailable — OpenAI client not configured.");
        }

        // --- Build prompt ---
        var prompt = BuildPrompt(recipe.Title, description, ingredientList, styleClause, freeText);
        _logger.LogDebug("Generating image for recipe {Id} with prompt: {Prompt}", recipeId, prompt);

        // --- Call OpenAI ---
        // gpt-image-1 always returns base64-encoded bytes — ResponseFormat is not supported.
        byte[] imageBytes;
        try
        {
            var imageClient = _openAi.GetImageClient(ImageModel);
            var options = new ImageGenerationOptions
            {
                Size = GeneratedImageSize.W1024xH1024,
            };

            var response = await imageClient.GenerateImageAsync(prompt, options);
            imageBytes = response.Value.ImageBytes?.ToArray()
                ?? throw new InvalidOperationException("OpenAI returned no image data.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI image generation failed for recipe {Id}.", recipeId);
            return ImageGenerationResult.OpenAiFailure($"Image generation failed: {ex.Message}");
        }

        // --- Store to volume ---
        var uploadsRoot = Path.Combine("/app", "uploads");
        var recipeDir = Path.Combine(uploadsRoot, "recipes", recipeId.ToString());
        Directory.CreateDirectory(recipeDir);

        // One image per recipe — clear any existing file
        foreach (var existing in Directory.GetFiles(recipeDir))
            File.Delete(existing);

        var safeFilename = $"{recipeId}.png";
        var fullPath = Path.Combine(recipeDir, safeFilename);
        await File.WriteAllBytesAsync(fullPath, imageBytes);

        // --- Update recipe ---
        var publicUrl = $"/uploads/recipes/{recipeId}/{safeFilename}";
        recipe.ImageUrl = publicUrl;
        recipe.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return ImageGenerationResult.Success(publicUrl);
    }

    // -----------------------------------------------------------------------
    // Prompt assembly (four layers in specified order)
    // -----------------------------------------------------------------------

    private static string BuildPrompt(
        string recipeTitle,
        string description,
        IReadOnlyList<string> ingredients,
        string styleClause,
        string? freeText)
    {
        var parts = new List<string>
        {
            // Layer 1 — base quality
            BaseQuality,
            // Layer 2 — style
            styleClause,
            // Layer 3 — recipe context
            $"A photograph of {recipeTitle} — {description}. Key ingredients: {string.Join(", ", ingredients)}.",
        };

        // Layer 4 — free text (optional)
        if (!string.IsNullOrWhiteSpace(freeText))
            parts.Add(freeText.Trim());

        return string.Join(" ", parts);
    }
}

// ---------------------------------------------------------------------------
// Result type (mirrors ImageUploadResult pattern)
// ---------------------------------------------------------------------------

/// <summary>
/// Result of an <see cref="ImageGenerationService.GenerateAsync"/> call.
/// </summary>
public class ImageGenerationResult
{
    public bool Succeeded { get; private init; }
    public bool RecipeNotFound { get; private init; }
    public bool IsOpenAiError { get; private init; }
    public bool IsStyleError { get; private init; }
    public string? PublicUrl { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static ImageGenerationResult Success(string url) =>
        new() { Succeeded = true, PublicUrl = url };

    public static ImageGenerationResult Failure(string message) =>
        new() { Succeeded = false, ErrorMessage = message };

    public static ImageGenerationResult InvalidStyle(string message) =>
        new() { Succeeded = false, IsStyleError = true, ErrorMessage = message };

    public static ImageGenerationResult OpenAiFailure(string message) =>
        new() { Succeeded = false, IsOpenAiError = true, ErrorMessage = message };

    public static ImageGenerationResult NotFound() =>
        new() { Succeeded = false, RecipeNotFound = true };
}
