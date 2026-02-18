namespace DocumentArchive.Core.Models;

public enum ActionType
{
    Created,
    Updated,
    Deleted,
    Viewed,
    Downloaded
}

public class ArchiveLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Action { get; set; } = string.Empty;
    public ActionType ActionType { get; set; }
    public bool IsCritical { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }
}