namespace DocumentArchive.Core.Models;

public class Permission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty; // например "CanEditDocument"
    public string? Description { get; set; }
    public string? Category { get; set; } // для группировки
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}