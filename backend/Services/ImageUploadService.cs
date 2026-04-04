using WalkerFcb.Api.Data;
using WalkerFcb.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace WalkerFcb.Api.Services;

/// <summary>
/// Handles file upload, validation, storage, and URL resolution for recipe images.
/// Files are stored under <c>/app/uploads/recipes/{recipeId}/</c> inside the container,
/// which maps to the <c>familycookbook_recipe_images</c> named Docker volume.
/// </summary>
public class ImageUploadService
{
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png"];
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png"];
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private readonly WalkerDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ImageUploadService(WalkerDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    /// <summary>
    /// Validates, stores, and records a recipe image upload.
    /// Returns a result containing the public URL on success, or an error message on failure.
    /// </summary>
    public async Task<ImageUploadResult> UploadAsync(int recipeId, IFormFile file)
    {
        // --- Validate recipe exists ---
        var recipe = await _db.Recipes.FirstOrDefaultAsync(r => r.Id == recipeId && r.DeletedAt == null);
        if (recipe == null)
            return ImageUploadResult.NotFound();

        // --- Validate file size ---
        if (file.Length == 0)
            return ImageUploadResult.Failure("File is empty.");

        if (file.Length > MaxFileSizeBytes)
            return ImageUploadResult.Failure($"File size exceeds the 5 MB limit.");

        // --- Validate content type ---
        var contentType = file.ContentType?.ToLowerInvariant() ?? string.Empty;
        if (!AllowedContentTypes.Contains(contentType))
            return ImageUploadResult.Failure("Only JPEG and PNG images are accepted.");

        // --- Validate extension ---
        var originalExtension = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? string.Empty;
        if (!AllowedExtensions.Contains(originalExtension))
            return ImageUploadResult.Failure("Only .jpg, .jpeg, and .png files are accepted.");

        // --- Resolve storage path ---
        // /app/uploads/ is the container path; it is mounted from the named Docker volume.
        var uploadsRoot = Path.Combine("/app", "uploads");
        var recipeDir = Path.Combine(uploadsRoot, "recipes", recipeId.ToString());

        Directory.CreateDirectory(recipeDir);

        // --- Delete any existing image for this recipe (one image per recipe) ---
        foreach (var existing in Directory.GetFiles(recipeDir))
        {
            File.Delete(existing);
        }

        // --- Sanitise filename and write file ---
        // Filename: {recipeId}{extension} — simple, collision-free, no user input in the path.
        var safeFilename = $"{recipeId}{originalExtension}";
        var fullPath = Path.Combine(recipeDir, safeFilename);

        await using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
        {
            await file.CopyToAsync(stream);
        }

        // --- Resolve public URL ---
        // nginx serves /uploads/ as a static path from the same mounted volume.
        var publicUrl = $"/uploads/recipes/{recipeId}/{safeFilename}";

        // --- Persist the URL back to the recipe ---
        recipe.ImageUrl = publicUrl;
        recipe.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return ImageUploadResult.Success(publicUrl);
    }
}

/// <summary>
/// Result of an <see cref="ImageUploadService.UploadAsync"/> call.
/// </summary>
public class ImageUploadResult
{
    public bool Succeeded { get; private init; }
    public bool RecipeNotFound { get; private init; }
    public string? PublicUrl { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static ImageUploadResult Success(string url) =>
        new() { Succeeded = true, PublicUrl = url };

    public static ImageUploadResult Failure(string message) =>
        new() { Succeeded = false, ErrorMessage = message };

    public static ImageUploadResult NotFound() =>
        new() { Succeeded = false, RecipeNotFound = true };
}
