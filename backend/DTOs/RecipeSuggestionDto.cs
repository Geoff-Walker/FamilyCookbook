namespace WalkerFcb.Api.DTOs;

// ---------------------------------------------------------------------------
// Shared response DTO
// ---------------------------------------------------------------------------

/// <summary>
/// Response DTO for a single recipe suggestion.
/// Returned by GET (list), POST (create), and PATCH /backlog.
/// <c>RecipeName</c> is populated via JOIN to recipes when <c>RecipeId</c> is set.
/// </summary>
public class RecipeSuggestionDto
{
    public int Id { get; set; }
    public int SuggestedBy { get; set; }
    public string SuggestedByName { get; set; } = string.Empty;
    public string? SuggestionUrl { get; set; }
    public string? SuggestionText { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? RecipeId { get; set; }
    public string? RecipeName { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ---------------------------------------------------------------------------
// POST /api/recipe-suggestions — request
// ---------------------------------------------------------------------------

/// <summary>
/// Request body for submitting a new recipe suggestion.
/// At least one of <c>SuggestionUrl</c> or <c>SuggestionText</c> must be non-empty.
/// Validation is enforced at the API layer — see <c>RecipeSuggestionService</c>.
/// </summary>
public class CreateRecipeSuggestionDto
{
    public int SuggestedBy { get; set; }
    public string? SuggestionUrl { get; set; }
    public string? SuggestionText { get; set; }
}

// ---------------------------------------------------------------------------
// POST /api/recipe-suggestions/{id}/accept — request and response
// ---------------------------------------------------------------------------

/// <summary>
/// Request body for the accept endpoint.
/// <c>RequestingUserId</c> must be 1 (Geoff) — enforced server-side; returns 403 otherwise.
/// </summary>
public class AcceptRecipeSuggestionDto
{
    public int RequestingUserId { get; set; }
}

/// <summary>
/// Response DTO for a successfully accepted suggestion.
/// </summary>
public class AcceptRecipeSuggestionResultDto
{
    public int SuggestionId { get; set; }
    public string Status { get; set; } = "accepted";
    public int RecipeId { get; set; }
    public string RecipeName { get; set; } = string.Empty;
}
