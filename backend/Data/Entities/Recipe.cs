using Pgvector;

namespace WalkerFcb.Api.Data.Entities;

/// <summary>
/// A family recipe. Soft-deleted via <see cref="DeletedAt"/>; never hard-deleted.
/// <see cref="Embedding"/> stores a 1536-dimension pgvector for semantic search.
/// Fluent API configuration is in <see cref="WalkerDbContext"/>.
/// </summary>
public class Recipe
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Summary { get; set; }
    public Vector? Embedding { get; set; }
    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }
    public int? Servings { get; set; }
    public string? Source { get; set; }
    /// <summary>
    /// URL of the hero image for this recipe.
    /// Currently accepts external URLs only.
    /// TODO (next sprint): replace with file upload — store locally or in object storage,
    /// keep this column as the resolved URL after upload.
    /// </summary>
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<RecipeStage> Stages { get; set; } = [];
    public ICollection<RecipeIngredient> Ingredients { get; set; } = [];
    public ICollection<RecipeTag> RecipeTags { get; set; } = [];
    public ICollection<RecipeReview> Reviews { get; set; } = [];
    public ICollection<CookInstance> CookInstances { get; set; } = [];
    public ICollection<RecipeVersion> RecipeVersions { get; set; } = [];
}
