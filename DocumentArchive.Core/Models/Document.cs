namespace DocumentArchive.Core.Models;

public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Связи
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }

    // Бизнес-логика 
    public bool CanBeDeleted() => (DateTime.UtcNow - UploadDate).TotalDays < 30;
}