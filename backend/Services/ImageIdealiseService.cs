using Microsoft.EntityFrameworkCore;
using OpenAI;
using OpenAI.Images;
using WalkerFcb.Api.Data;

namespace WalkerFcb.Api.Services;

/// <summary>
/// Idealises an existing recipe image via OpenAI gpt-image-1 images.edit (img2img).
/// Reads the source image from the local upload volume or fetches it remotely for
/// legacy seeded URLs, then stores the result to the same Docker volume used by
/// <see cref="ImageUploadService"/> and <see cref="ImageGenerationService"/>.
/// </summary>
public class ImageIdealiseService
{
    private const string ImageModel = "gpt-image-1";
    private const string BaseQuality = "Professional food photography, appetising, high resolution, natural light, shallow depth of field, realistic";
    private const string TransformDirective = "Transform this photograph into a professional cookbook image using the style above. Preserve the dish and ingredients exactly.";

    private readonly WalkerDbContext _db;
    private readonly OpenAIClient? _openAi;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ImageIdealiseService> _logger;

    public ImageIdealiseService(
        WalkerDbContext db,
        OpenAIClient? openAi,
        HttpClient httpClient,
        ILogger<ImageIdealiseService> logger)
    {
        _db = db;
        _openAi = openAi;
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Assembles a prompt, fetches the source image, calls gpt-image-1 images.edit,
    /// downloads the result, stores it to the image volume, updates
    /// <c>recipes.image_url</c>, and returns the public URL.
    /// </summary>
    public async Task<ImageIdealiseResult> IdealiseAsync(
        int recipeId,
        string style,
        string? freeText)
    {
        // --- Recipe lookup ---
        var recipe = await _db.Recipes.FirstOrDefaultAsync(r => r.Id == recipeId && r.DeletedAt == null);
        if (recipe is null)
            return ImageIdealiseResult.NotFound();

        // --- Image guard ---
        if (string.IsNullOrWhiteSpace(recipe.ImageUrl))
            return ImageIdealiseResult.NoImage();

        // --- Style validation ---
        var styleClause = RecipeImageStyles.GetClause(style);
        if (styleClause is null)
            return ImageIdealiseResult.InvalidStyle(
                $"style must be one of: {string.Join(", ", RecipeImageStyles.ValidStyles)}.");

        // --- OpenAI client guard ---
        if (_openAi is null)
        {
            _logger.LogWarning("Image idealise requested for recipe {Id} but OpenAI client is not configured.", recipeId);
            return ImageIdealiseResult.Failure("Image idealise is unavailable — OpenAI client not configured.");
        }

        // --- Resolve source image bytes ---
        byte[] sourceBytes;
        string sourceFilename;
        try
        {
            (sourceBytes, sourceFilename) = await ResolveSourceImageAsync(recipe.ImageUrl, recipeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve source image for recipe {Id} from {Url}.", recipeId, recipe.ImageUrl);
            return ImageIdealiseResult.Failure($"Could not load source image: {ex.Message}");
        }

        // --- Build prompt ---
        var prompt = BuildPrompt(styleClause, freeText);
        _logger.LogDebug("Idealising image for recipe {Id} with prompt: {Prompt}", recipeId, prompt);

        // --- Call OpenAI images.edit ---
        string resultImageUrl;
        try
        {
            var imageClient = _openAi.GetImageClient(ImageModel);
            var options = new ImageEditOptions
            {
                Size = GeneratedImageSize.W1024xH1024,
                ResponseFormat = GeneratedImageFormat.Uri,
            };

            using var imageStream = new MemoryStream(sourceBytes);
            var response = await imageClient.GenerateImageEditAsync(
                imageStream,
                sourceFilename,
                prompt,
                options,
                CancellationToken.None);

            resultImageUrl = response.Value.ImageUri?.ToString()
                ?? throw new InvalidOperationException("OpenAI returned no image URI.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI image edit failed for recipe {Id}.", recipeId);
            return ImageIdealiseResult.OpenAiFailure($"Image idealise failed: {ex.Message}");
        }

        // --- Download result bytes ---
        byte[] resultBytes;
        try
        {
            resultBytes = await _httpClient.GetByteArrayAsync(resultImageUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download idealised image for recipe {Id} from {Url}.", recipeId, resultImageUrl);
            return ImageIdealiseResult.OpenAiFailure("Idealised image could not be downloaded.");
        }

        // --- Store to volume (same convention as WAL-32 / WAL-34) ---
        var uploadsRoot = Path.Combine("/app", "uploads");
        var recipeDir = Path.Combine(uploadsRoot, "recipes", recipeId.ToString());
        Directory.CreateDirectory(recipeDir);

        // One image per recipe — replace any existing file
        foreach (var existing in Directory.GetFiles(recipeDir))
            File.Delete(existing);

        var safeFilename = $"{recipeId}.png";
        var fullPath = Path.Combine(recipeDir, safeFilename);
        await File.WriteAllBytesAsync(fullPath, resultBytes);

        // --- Update recipe ---
        var publicUrl = $"/uploads/recipes/{recipeId}/{safeFilename}";
        recipe.ImageUrl = publicUrl;
        recipe.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return ImageIdealiseResult.Success(publicUrl);
    }

    // -----------------------------------------------------------------------
    // Source image resolution
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns the raw bytes and a filename for the source image.
    /// Local /uploads/ paths are read directly from the Docker volume.
    /// External URLs (legacy seeded data) are fetched via HttpClient.
    /// </summary>
    private async Task<(byte[] bytes, string filename)> ResolveSourceImageAsync(
        string imageUrl,
        int recipeId)
    {
        if (imageUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            // Local volume path: /uploads/recipes/{id}/... → /app/uploads/recipes/{id}/...
            var diskPath = Path.Combine("/app", imageUrl.TrimStart('/'));
            var bytes = await File.ReadAllBytesAsync(diskPath);
            var filename = Path.GetFileName(diskPath);
            return (bytes, filename);
        }
        else
        {
            // External URL — fetch remotely
            var bytes = await _httpClient.GetByteArrayAsync(imageUrl);
            // Derive a safe filename from the URL, defaulting to png
            var uriFilename = Path.GetFileName(new Uri(imageUrl).LocalPath);
            var filename = string.IsNullOrWhiteSpace(uriFilename) ? $"{recipeId}.png" : uriFilename;
            return (bytes, filename);
        }
    }

    // -----------------------------------------------------------------------
    // Prompt assembly (three layers + optional free text, in order)
    // -----------------------------------------------------------------------

    private static string BuildPrompt(string styleClause, string? freeText)
    {
        var parts = new List<string>
        {
            // Layer 1 — base quality
            BaseQuality,
            // Layer 2 — style injection
            styleClause,
            // Layer 3 — transformation directive
            TransformDirective,
        };

        // Layer 4 — optional free text
        if (!string.IsNullOrWhiteSpace(freeText))
            parts.Add(freeText.Trim());

        return string.Join(" ", parts);
    }
}

// ---------------------------------------------------------------------------
// Result type (mirrors ImageGenerationResult pattern)
// ---------------------------------------------------------------------------

/// <summary>
/// Result of an <see cref="ImageIdealiseService.IdealiseAsync"/> call.
/// </summary>
public class ImageIdealiseResult
{
    public bool Succeeded { get; private init; }
    public bool RecipeNotFound { get; private init; }
    public bool IsNoImageError { get; private init; }
    public bool IsOpenAiError { get; private init; }
    public bool IsStyleError { get; private init; }
    public string? PublicUrl { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static ImageIdealiseResult Success(string url) =>
        new() { Succeeded = true, PublicUrl = url };

    public static ImageIdealiseResult Failure(string message) =>
        new() { Succeeded = false, ErrorMessage = message };

    public static ImageIdealiseResult NoImage() =>
        new() { Succeeded = false, IsNoImageError = true, ErrorMessage = "No image to idealise. Upload an image first." };

    public static ImageIdealiseResult InvalidStyle(string message) =>
        new() { Succeeded = false, IsStyleError = true, ErrorMessage = message };

    public static ImageIdealiseResult OpenAiFailure(string message) =>
        new() { Succeeded = false, IsOpenAiError = true, ErrorMessage = message };

    public static ImageIdealiseResult NotFound() =>
        new() { Succeeded = false, RecipeNotFound = true };
}
