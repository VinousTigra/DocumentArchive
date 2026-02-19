namespace DocumentArchive.Core.DTOs.DocumentVersion;

public class DocumentVersionListItemDto
{
    public Guid Id { get; set; }
    public int VersionNumber { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}