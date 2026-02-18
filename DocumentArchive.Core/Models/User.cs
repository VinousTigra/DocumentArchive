namespace DocumentArchive.Core.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Поля для аутентификации (ЛР4)
    public string? PasswordHash { get; set; }
    public string? PasswordSalt { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } // soft delete

    // Навигационные свойства
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<ArchiveLog> Logs { get; set; } = new List<ArchiveLog>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}