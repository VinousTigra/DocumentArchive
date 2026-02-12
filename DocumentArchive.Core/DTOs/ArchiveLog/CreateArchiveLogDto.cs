using DocumentArchive.Core.Models;

namespace DocumentArchive.Core.DTOs.ArchiveLog;

public class CreateArchiveLogDto
{
    public string Action { get; set; } = string.Empty;
    public ActionType ActionType { get; set; }
    public bool IsCritical { get; set; }
    public Guid UserId { get; set; }
    public Guid DocumentId { get; set; }
}