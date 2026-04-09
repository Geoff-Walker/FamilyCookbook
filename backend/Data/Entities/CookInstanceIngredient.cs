namespace WalkerFcb.Api.Data.Entities;

/// <summary>
/// Records the actual quantity of an ingredient used during a <see cref="CookInstance"/>.
/// <see cref="Amount"/> is the numeric quantity; <see cref="UnitId"/> is nullable to allow
/// unit-free entries. <see cref="IsLimiter"/> flags the ingredient that constrained the
/// portion size for this cook.
/// Fluent API configuration is in <see cref="WalkerDbContext"/>.
/// </summary>
public class CookInstanceIngredient
{
    public int Id { get; set; }
    public int CookInstanceId { get; set; }
    public int IngredientId { get; set; }
    public decimal Amount { get; set; }
    public int? UnitId { get; set; }
    public bool Checked { get; set; }
    public bool IsLimiter { get; set; }
    public string? Notes { get; set; }

    public CookInstance CookInstance { get; set; } = null!;
    public Ingredient Ingredient { get; set; } = null!;
    public Unit? Unit { get; set; }
}
