namespace WalkerFcb.Api.Data.Entities;

/// <summary>
/// Minimal stub — full entity definition in WAL-27.
/// </summary>
public class RecipeStep
{
    public int Id { get; set; }
    public int StageId { get; set; }
    public string Instruction { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public RecipeStage Stage { get; set; } = null!;
}
