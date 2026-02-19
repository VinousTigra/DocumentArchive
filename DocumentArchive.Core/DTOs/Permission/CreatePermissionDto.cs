namespace DocumentArchive.Core.DTOs.Permission;

public class CreatePermissionDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
}