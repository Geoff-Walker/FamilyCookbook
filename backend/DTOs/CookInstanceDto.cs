namespace WalkerFcb.Api.DTOs;

// ---------------------------------------------------------------------------
// POST /api/cook-instances — request
// ---------------------------------------------------------------------------

/// <summary>
/// Request body for starting a new cook instance.
/// </summary>
public class StartCookDto
{
    public int RecipeId { get; set; }
    public int UserId { get; set; }
    public int? Portions { get; set; }
    public string? Notes { get; set; }
}

// ---------------------------------------------------------------------------
// GET /api/cook-instances/{id} — response
// ---------------------------------------------------------------------------

/// <summary>
/// Full cook instance detail, with ingredients grouped by stage.
/// </summary>
public class CookInstanceDetailDto
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public string RecipeTitle { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? Portions { get; set; }
    public string? Notes { get; set; }
    public List<CookInstanceStageGroupDto> StageGroups { get; set; } = [];
}

/// <summary>
/// A stage group within a cook instance. StageName is null for whole-recipe
/// (unstaged) ingredients.
/// </summary>
public class CookInstanceStageGroupDto
{
    public int? StageId { get; set; }
    public string? StageName { get; set; }
    public int SortOrder { get; set; }
    public List<CookInstanceIngredientDto> Ingredients { get; set; } = [];
}

/// <summary>
/// An individual ingredient line within an active cook instance.
/// </summary>
public class CookInstanceIngredientDto
{
    public int Id { get; set; }
    public int IngredientId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int? UnitId { get; set; }
    public string? UnitName { get; set; }
    public string? UnitAbbreviation { get; set; }
    public bool Checked { get; set; }
    public bool IsLimiter { get; set; }
    public string? Notes { get; set; }
}

// ---------------------------------------------------------------------------
// PATCH /api/cook-instances/{id}/ingredients/{ingredientId} — request
// ---------------------------------------------------------------------------

/// <summary>
/// Partial update for a cook instance ingredient. All fields are optional —
/// only non-null values are applied.
/// </summary>
public class PatchCookInstanceIngredientDto
{
    public bool? Checked { get; set; }
    public decimal? Amount { get; set; }
    public bool? IsLimiter { get; set; }
}

// ---------------------------------------------------------------------------
// POST /api/cook-instances/{id}/complete — request
// ---------------------------------------------------------------------------

/// <summary>
/// Request body for completing a cook instance. Reviews are optional.
/// </summary>
public class CompleteCookDto
{
    public int? Portions { get; set; }
    public string? Notes { get; set; }
    public List<CookReviewDto> Reviews { get; set; } = [];
}

/// <summary>
/// A single user's review submitted as part of completing a cook.
/// </summary>
public class CookReviewDto
{
    public int UserId { get; set; }
    public decimal Rating { get; set; }
    public string? Notes { get; set; }
}

// ---------------------------------------------------------------------------
// GET /api/recipes/{recipeId}/cook-instances — history list item
// ---------------------------------------------------------------------------

/// <summary>
/// Summary row for the cook history list. Includes ratings per user.
/// </summary>
public class CookInstanceSummaryDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? Portions { get; set; }
    public string? Notes { get; set; }
    public List<CookInstanceReviewSummaryDto> Reviews { get; set; } = [];
}

/// <summary>
/// A single review row within the cook history summary.
/// </summary>
public class CookInstanceReviewSummaryDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public string? Notes { get; set; }
}
