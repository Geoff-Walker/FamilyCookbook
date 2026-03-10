namespace WalkerFcb.Api.Data.Entities;

/// <summary>
/// Minimal stub — full entity definition in WAL-27.
/// Join table with composite PK (RecipeId, TagId).
/// </summary>
public class RecipeTag
{
    public int RecipeId { get; set; }
    public int TagId { get; set; }

    public Recipe Recipe { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
