namespace DocumentArchive.Core.Models;

public class ArchiveLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Action { get; set; } = string.Empty; // "Created", "Updated", "Deleted"
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    // Связи
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    
    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }
}