namespace DocumentArchive.Core.DTOs.Document;

// DocumentListItemDto.cs (для списка)
public class DocumentListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
}