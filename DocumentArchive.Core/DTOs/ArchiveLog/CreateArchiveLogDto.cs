namespace DocumentArchive.Core.DTOs.ArchiveLog;

public class CreateArchiveLogDto
{
    public string Action { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid DocumentId { get; set; }
}