using WalkerFcb.Api.Data.Entities;

namespace WalkerFcb.Api.Services;

/// <summary>
/// Stub implementation of the recipe embedding pipeline.
/// WAL-22 will replace this with the real OpenAI embedding call.
/// Returns (null, null) so callers can treat AI failure as non-fatal.
/// </summary>
public class RecipeEmbeddingService
{
    /// <summary>
    /// Generates an AI summary and embedding vector for the given recipe.
    /// This stub always returns (null, null) — real implementation is WAL-22.
    /// </summary>
    public Task<(string? Summary, float[]? Embedding)> GenerateAsync(Recipe recipe)
    {
        return Task.FromResult<(string?, float[]?)>((null, null));
    }
}
