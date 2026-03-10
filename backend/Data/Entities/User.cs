namespace WalkerFcb.Api.Data.Entities;

/// <summary>
/// Minimal stub — full entity definition in WAL-27.
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<RecipeReview> Reviews { get; set; } = [];
}
