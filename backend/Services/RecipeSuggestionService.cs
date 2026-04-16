using Microsoft.EntityFrameworkCore;
using WalkerFcb.Api.Data;
using WalkerFcb.Api.Data.Entities;
using WalkerFcb.Api.DTOs;

namespace WalkerFcb.Api.Services;

/// <summary>
/// Business logic for The Geoff Filter — the recipe suggestion queue.
/// Suggestions move through: pending → accepted / deleted / backlogged.
/// Only user ID 1 (Geoff) can accept suggestions — enforced here, not at the DB layer.
/// </summary>
public class RecipeSuggestionService
{
    private readonly WalkerDbContext _db;

    public RecipeSuggestionService(WalkerDbContext db)
    {
        _db = db;
    }

    // -----------------------------------------------------------------------
    // GET — list by status
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns all suggestions with the given status, ordered by created_at ASC.
    /// Returns an empty list if none exist.
    /// </summary>
    public async Task<List<RecipeSuggestionDto>> GetByStatusAsync(string status)
    {
        var suggestions = await _db.RecipeSuggestions
            .AsNoTracking()
            .Where(s => s.Status == status)
            .Include(s => s.SuggestedByUser)
            .Include(s => s.Recipe)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();

        return suggestions.Select(ToDto).ToList();
    }

    // -----------------------------------------------------------------------
    // POST — create
    // -----------------------------------------------------------------------

    /// <summary>
    /// Validates and creates a new suggestion with status 'pending'.
    /// Returns a non-null <c>ValidationError</c> string if both url and text are null/empty.
    /// Returns <c>NotFound = true</c> if the submitting user does not exist.
    /// </summary>
    public async Task<(RecipeSuggestionDto? Dto, string? ValidationError, bool NotFound)> CreateAsync(
        CreateRecipeSuggestionDto request)
    {
        var urlEmpty = string.IsNullOrWhiteSpace(request.SuggestionUrl);
        var textEmpty = string.IsNullOrWhiteSpace(request.SuggestionText);

        if (urlEmpty && textEmpty)
            return (null, "At least one of suggestionUrl or suggestionText must be provided.", false);

        var user = await _db.Users.FindAsync(request.SuggestedBy);
        if (user == null)
            return (null, null, true);

        var now = DateTime.UtcNow;
        var suggestion = new RecipeSuggestion
        {
            SuggestedBy = request.SuggestedBy,
            SuggestionUrl = string.IsNullOrWhiteSpace(request.SuggestionUrl) ? null : request.SuggestionUrl.Trim(),
            SuggestionText = string.IsNullOrWhiteSpace(request.SuggestionText) ? null : request.SuggestionText.Trim(),
            Status = "pending",
            RecipeId = null,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.RecipeSuggestions.Add(suggestion);
        await _db.SaveChangesAsync();

        // Reload with navigation properties for the response DTO
        suggestion.SuggestedByUser = user;

        return (ToDto(suggestion), null, false);
    }

    // -----------------------------------------------------------------------
    // PATCH — backlog
    // -----------------------------------------------------------------------

    /// <summary>
    /// Sets the suggestion status to 'backlogged' and updates updated_at.
    /// Returns null if the suggestion is not found or is already deleted.
    /// </summary>
    public async Task<RecipeSuggestionDto?> BacklogAsync(int id)
    {
        var suggestion = await _db.RecipeSuggestions
            .Include(s => s.SuggestedByUser)
            .Include(s => s.Recipe)
            .FirstOrDefaultAsync(s => s.Id == id && s.Status != "deleted");

        if (suggestion == null)
            return null;

        suggestion.Status = "backlogged";
        suggestion.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return ToDto(suggestion);
    }

    // -----------------------------------------------------------------------
    // DELETE — soft delete
    // -----------------------------------------------------------------------

    /// <summary>
    /// Soft-deletes the suggestion by setting status to 'deleted'.
    /// Returns false if the suggestion is not found or is already deleted.
    /// No ownership check — either user may delete any suggestion.
    /// </summary>
    public async Task<bool> SoftDeleteAsync(int id)
    {
        var suggestion = await _db.RecipeSuggestions
            .FirstOrDefaultAsync(s => s.Id == id && s.Status != "deleted");

        if (suggestion == null)
            return false;

        suggestion.Status = "deleted";
        suggestion.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return true;
    }

    // -----------------------------------------------------------------------
    // POST — accept
    // -----------------------------------------------------------------------

    /// <summary>
    /// Accepts a suggestion — restricted to user ID 1 (Geoff).
    /// Creates a stub recipe, links it to the suggestion, and sets status to 'accepted'.
    /// Returns <c>Forbidden = true</c> if <paramref name="requestingUserId"/> is not 1.
    /// Returns <c>NotFound = true</c> if the suggestion is not found or is already deleted.
    /// </summary>
    public async Task<(AcceptRecipeSuggestionResultDto? Result, bool Forbidden, bool NotFound)> AcceptAsync(
        int id,
        int requestingUserId)
    {
        if (requestingUserId != 1)
            return (null, true, false);

        var suggestion = await _db.RecipeSuggestions
            .FirstOrDefaultAsync(s => s.Id == id && s.Status != "deleted");

        if (suggestion == null)
            return (null, false, true);

        // Build stub recipe title: prefer suggestion text (first 100 chars),
        // fall back to URI hostname if text is null/empty.
        var title = BuildStubTitle(suggestion.SuggestionText, suggestion.SuggestionUrl);

        var now = DateTime.UtcNow;
        var recipe = new Recipe
        {
            Title = title,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Recipes.Add(recipe);
        await _db.SaveChangesAsync(); // get the new recipe ID

        suggestion.Status = "accepted";
        suggestion.RecipeId = recipe.Id;
        suggestion.UpdatedAt = now;

        await _db.SaveChangesAsync();

        return (new AcceptRecipeSuggestionResultDto
        {
            SuggestionId = suggestion.Id,
            Status = "accepted",
            RecipeId = recipe.Id,
            RecipeName = recipe.Title
        }, false, false);
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private static string BuildStubTitle(string? suggestionText, string? suggestionUrl)
    {
        if (!string.IsNullOrWhiteSpace(suggestionText))
        {
            var trimmed = suggestionText.Trim();
            return trimmed.Length <= 100 ? trimmed : trimmed[..100];
        }

        if (!string.IsNullOrWhiteSpace(suggestionUrl)
            && Uri.TryCreate(suggestionUrl, UriKind.Absolute, out var uri))
        {
            return uri.Host;
        }

        return "Untitled Recipe";
    }

    private static RecipeSuggestionDto ToDto(RecipeSuggestion s) => new()
    {
        Id = s.Id,
        SuggestedBy = s.SuggestedBy,
        SuggestedByName = s.SuggestedByUser?.Name ?? string.Empty,
        SuggestionUrl = s.SuggestionUrl,
        SuggestionText = s.SuggestionText,
        Status = s.Status,
        RecipeId = s.RecipeId,
        RecipeName = s.Recipe?.Title,
        CreatedAt = s.CreatedAt
    };
}
