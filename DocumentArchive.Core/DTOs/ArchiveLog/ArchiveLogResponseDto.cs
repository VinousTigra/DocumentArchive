using DocumentArchive.Core.Models;

namespace DocumentArchive.Core.DTOs.ArchiveLog;

public class ArchiveLogResponseDto
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public ActionType ActionType { get; set; }
    public bool IsCritical { get; set; }
    public DateTime Timestamp { get; set; }
    public string? UserName { get; set; }
    public string? DocumentTitle { get; set; }
}