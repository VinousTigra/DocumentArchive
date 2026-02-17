using DocumentArchive.Core.DTOs.Category;
using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.DTOs.Shared;

namespace DocumentArchive.Core.Interfaces.Services;

public interface ICategoryService
{
    Task<PagedResult<CategoryListItemDto>> GetCategoriesAsync(
        int page,
        int pageSize,
        string? search,
        string? sortBy,
        string? sortOrder,
        CancellationToken cancellationToken = default);

    Task<CategoryResponseDto?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CategoryResponseDto> CreateCategoryAsync(CreateCategoryDto createDto, CancellationToken cancellationToken = default);
    Task UpdateCategoryAsync(Guid id, UpdateCategoryDto updateDto, CancellationToken cancellationToken = default);
    Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Получает документы категории с пагинацией
    /// </summary>
    Task<PagedResult<DocumentListItemDto>> GetCategoryDocumentsAsync(
        Guid categoryId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}