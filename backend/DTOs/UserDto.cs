namespace WalkerFcb.Api.DTOs;

/// <summary>
/// Read-only DTO returned by GET /api/users.
/// </summary>
public class UserDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
