using WalkerFcb.Api.Data.Entities;
using WalkerFcb.Api.DTOs;

namespace WalkerFcb.Api.Data.Repositories;

/// <summary>
/// Data access contract for the <see cref="Recipe"/> aggregate.
/// All methods operate only on non-deleted recipes (soft delete filter applied at DbContext level).
/// </summary>
public interface IRecipeRepository
{
    /// <summary>
    /// Returns a summary projection of all non-deleted recipes, including tags and
    /// average rating, ordered by creation date descending.
    /// </summary>
    Task<List<RecipeSummary>> GetAllAsync();

    /// <summary>
    /// Returns the full recipe with all associated data loaded (stages, steps,
    /// ingredients, tags, reviews), or <c>null</c> if not found or soft-deleted.
    /// </summary>
    Task<Recipe?> GetByIdAsync(int id);

    /// <summary>
    /// Persists a new recipe and returns it with its generated <see cref="Recipe.Id"/> populated.
    /// </summary>
    Task<Recipe> CreateAsync(Recipe recipe);

    /// <summary>
    /// Replaces child collections on the tracked recipe and saves all changes.
    /// The caller is responsible for providing the full updated state.
    /// </summary>
    Task<Recipe> UpdateAsync(Recipe recipe);

    /// <summary>
    /// Soft-deletes a recipe by setting <see cref="Recipe.DeletedAt"/> to UTC now.
    /// Throws <see cref="KeyNotFoundException"/> if the recipe does not exist.
    /// </summary>
    Task SoftDeleteAsync(int id);
}
