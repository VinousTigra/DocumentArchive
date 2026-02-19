namespace DocumentArchive.Core.DTOs.Permission;

public class PermissionListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
}