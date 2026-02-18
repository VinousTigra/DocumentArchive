using DocumentArchive.Core.DTOs.Document;

namespace DocumentArchive.Core.DTOs.Statistics;

public class DocumentsStatisticsDto
{
    public int TotalDocuments { get; set; }
    public List<CategoryCountDto> DocumentsPerCategory { get; set; } = new();
    public DocumentListItemDto? LastUploadedDocument { get; set; }
}

public class CategoryCountDto
{
    public string CategoryName { get; set; } = string.Empty;
    public int Count { get; set; }
}