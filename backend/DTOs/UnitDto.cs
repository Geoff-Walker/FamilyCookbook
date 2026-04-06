namespace WalkerFcb.Api.DTOs;

/// <summary>
/// Read-only DTO returned by GET /api/units.
/// </summary>
public class UnitDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Abbreviation { get; init; }
    public string UnitType { get; init; } = string.Empty;
}

/// <summary>
/// Request body for POST /api/units.
/// </summary>
public class CreateUnitDto
{
    public string Name { get; init; } = string.Empty;
    public string? Abbreviation { get; init; }
}
