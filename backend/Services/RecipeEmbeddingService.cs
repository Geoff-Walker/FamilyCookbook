using OpenAI;
using WalkerFcb.Api.Data.Entities;

namespace WalkerFcb.Api.Services;

/// <summary>
/// Generates an embedding vector for a recipe using OpenAI text-embedding-3-small.
/// The recipe text is assembled from its title, description, ingredients, and method steps.
/// If the OpenAI client is not configured, returns (null, null) so the save still succeeds.
/// </summary>
public class RecipeEmbeddingService
{
    private const string EmbeddingModel = "text-embedding-3-small";

    private readonly OpenAIClient? _openAi;
    private readonly ILogger<RecipeEmbeddingService> _logger;

    public RecipeEmbeddingService(OpenAIClient? openAi, ILogger<RecipeEmbeddingService> logger)
    {
        _openAi = openAi;
        _logger = logger;
    }

    /// <summary>
    /// Builds a text document from the recipe and embeds it via OpenAI.
    /// Returns (null, null) if the OpenAI client is not configured or the call fails.
    /// </summary>
    public async Task<(string? Summary, float[]? Embedding)> GenerateAsync(Recipe recipe)
    {
        if (_openAi is null)
        {
            _logger.LogDebug("Skipping embedding for recipe {Id} — OpenAI client not configured.", recipe.Id);
            return (null, null);
        }

        var text = BuildRecipeText(recipe);

        try
        {
            var client = _openAi.GetEmbeddingClient(EmbeddingModel);
            var response = await client.GenerateEmbeddingAsync(text);
            var vector = response.Value.ToFloats().ToArray();
            return (null, vector);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Embedding generation failed for recipe {Id} '{Title}'.", recipe.Id, recipe.Title);
            return (null, null);
        }
    }

    /// <summary>
    /// Assembles a compact plain-text representation of the recipe suitable for embedding.
    /// Format: title, optional description, ingredient list, then method steps by stage.
    /// </summary>
    private static string BuildRecipeText(Recipe recipe)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine(recipe.Title);

        if (!string.IsNullOrWhiteSpace(recipe.Description))
            sb.AppendLine(recipe.Description);

        // Ingredients — flatten across all stages, ordered by sort position
        var ingredients = recipe.Ingredients
            .OrderBy(i => i.SortOrder)
            .Select(i =>
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(i.Amount)) parts.Add(i.Amount);
                if (i.Unit != null) parts.Add(i.Unit.Abbreviation ?? i.Unit.Name);
                parts.Add(i.Ingredient.Name);
                if (!string.IsNullOrWhiteSpace(i.Notes)) parts.Add($"({i.Notes})");
                return string.Join(" ", parts);
            })
            .ToList();

        if (ingredients.Count > 0)
            sb.AppendLine("Ingredients: " + string.Join(", ", ingredients));

        // Method — stages with their steps
        foreach (var stage in recipe.Stages.OrderBy(s => s.SortOrder))
        {
            if (!string.IsNullOrWhiteSpace(stage.Name))
                sb.AppendLine(stage.Name + ":");

            foreach (var step in stage.Steps.OrderBy(s => s.SortOrder))
                sb.AppendLine(step.Instruction);
        }

        return sb.ToString().Trim();
    }
}
