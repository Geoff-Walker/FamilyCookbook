namespace WalkerFcb.Api.Data.Entities;

/// <summary>
/// A single instructional step within a <see cref="RecipeStage"/>.
/// Fluent API configuration is in <see cref="WalkerDbContext"/>.
/// </summary>
public class RecipeStep
{
    public int Id { get; set; }
    public int StageId { get; set; }
    public string Instruction { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public RecipeStage Stage { get; set; } = null!;
}
