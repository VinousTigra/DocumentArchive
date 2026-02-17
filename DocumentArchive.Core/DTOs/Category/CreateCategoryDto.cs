namespace DocumentArchive.Core.DTOs.Category;

public class CreateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}