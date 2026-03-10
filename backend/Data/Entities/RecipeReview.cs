namespace WalkerFcb.Api.Data.Entities;

/// <summary>
/// Minimal stub — full entity definition in WAL-27.
/// </summary>
public class RecipeReview
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public int UserId { get; set; }
    public int Rating { get; set; }
    public string? Notes { get; set; }
    public DateOnly? MadeOn { get; set; }
    public DateTime CreatedAt { get; set; }

    public Recipe Recipe { get; set; } = null!;
    public User User { get; set; } = null!;
}
