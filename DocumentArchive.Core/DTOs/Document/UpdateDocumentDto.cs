namespace DocumentArchive.Core.DTOs.Document;

public class UpdateDocumentDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? FileName { get; set; }
    public Guid? CategoryId { get; set; }
}