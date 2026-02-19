namespace DocumentArchive.Core.DTOs.Role;

public class CreateRoleDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}