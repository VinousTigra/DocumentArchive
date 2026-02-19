namespace DocumentArchive.Core.DTOs.DocumentVersion;

public class CreateDocumentVersionDto
{
    public Guid DocumentId { get; set; }
    public int VersionNumber { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? Comment { get; set; }
    public Guid? UploadedBy { get; set; }
}