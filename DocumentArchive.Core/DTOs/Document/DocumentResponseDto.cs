namespace DocumentArchive.Core.DTOs.Document;

// DocumentResponseDto.cs
public class DocumentResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CategoryName { get; set; }
    public string? UserName { get; set; }
}