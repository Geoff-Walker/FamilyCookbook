namespace WalkerFcb.Api.DTOs;

/// <summary>
/// Read-only DTO returned by GET /api/tag-categories.
/// </summary>
public class TagCategoryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
