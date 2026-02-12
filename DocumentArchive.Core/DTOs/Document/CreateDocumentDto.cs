namespace DocumentArchive.Core.DTOs.Document;

public class CreateDocumentDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FileName { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public Guid? UserId { get; set; }
}