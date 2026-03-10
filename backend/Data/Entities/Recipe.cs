using Pgvector;

namespace WalkerFcb.Api.Data.Entities;

/// <summary>
/// Minimal stub — full entity definition in WAL-27.
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
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<RecipeStage> Stages { get; set; } = [];
    public ICollection<RecipeIngredient> Ingredients { get; set; } = [];
    public ICollection<RecipeTag> RecipeTags { get; set; } = [];
    public ICollection<RecipeReview> Reviews { get; set; } = [];
}
