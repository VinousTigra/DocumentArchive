namespace DocumentArchive.Core.DTOs.Statistics;

public class CategoryWithDocumentCountDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DocumentsCount { get; set; }
}