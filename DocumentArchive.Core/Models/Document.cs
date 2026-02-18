namespace DocumentArchive.Core.Models;

public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Внешние ключи
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }

    // Навигационные свойства для новых сущностей
    public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
    public ICollection<ArchiveLog> Logs { get; set; } = new List<ArchiveLog>(); // добавлено для связи с ArchiveLog

    // Бизнес-логика
    public bool CanBeDeleted() => (DateTime.UtcNow - UploadDate).TotalDays < 30;
}