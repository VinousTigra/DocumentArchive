namespace DocumentArchive.Core.Models;

public class Permission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } // добавить

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}