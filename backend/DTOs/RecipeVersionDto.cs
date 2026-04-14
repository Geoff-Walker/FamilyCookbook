namespace WalkerFcb.Api.DTOs;

// ---------------------------------------------------------------------------
// POST /api/cook-instances/{id}/promote — response
// ---------------------------------------------------------------------------

/// <summary>
/// Response returned after a successful promote-to-recipe operation.
/// </summary>
public class PromoteResultDto
{
    public int VersionId { get; set; }
    public int VersionNumber { get; set; }
    public int RecipeId { get; set; }
    public DateTime PromotedAt { get; set; }
}

// ---------------------------------------------------------------------------
// GET /api/recipes/{recipeId}/versions — list item
// ---------------------------------------------------------------------------

/// <summary>
/// Summary row for a single recipe version in the version history list.
/// </summary>
public class RecipeVersionSummaryDto
{
    public int Id { get; set; }
    public int VersionNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedByName { get; set; }
    /// <summary>
    /// Date portion of the cook instance's started_at, or null if not promoted from a cook.
    /// </summary>
    public DateOnly? PromotedFromCookDate { get; set; }
}
