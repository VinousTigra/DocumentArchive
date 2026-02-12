namespace DocumentArchive.Core.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation property
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<ArchiveLog> Logs { get; set; } = new List<ArchiveLog>();
}