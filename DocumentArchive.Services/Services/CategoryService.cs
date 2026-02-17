using AutoMapper;
using DocumentArchive.Core.DTOs.Category;
using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.Interfaces.Repositorys;
using DocumentArchive.Core.Interfaces.Services;
using DocumentArchive.Core.Models;
using Microsoft.Extensions.Logging;

namespace DocumentArchive.Services.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepo;
    private readonly IDocumentRepository _documentRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(
        ICategoryRepository categoryRepo,
        IDocumentRepository documentRepo,
        IMapper mapper,
        ILogger<CategoryService> logger)
    {
        _categoryRepo = categoryRepo;
        _documentRepo = documentRepo;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResult<CategoryListItemDto>> GetCategoriesAsync(
        int page,
        int pageSize,
        string? search,
        string? sortBy,
        string? sortOrder,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting categories page {Page} size {PageSize}, search '{Search}'", page, pageSize, search);
        var paged = await _categoryRepo.GetPagedAsync(page, pageSize, search, sortBy, sortOrder, cancellationToken);
        var dtos = _mapper.Map<List<CategoryListItemDto>>(paged.Items);
        return new PagedResult<CategoryListItemDto>
        {
            Items = dtos,
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount
        };
    }

    public async Task<CategoryResponseDto?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepo.GetByIdAsync(id, cancellationToken);
        return category == null ? null : _mapper.Map<CategoryResponseDto>(category);
    }

    public async Task<CategoryResponseDto> CreateCategoryAsync(CreateCategoryDto createDto, CancellationToken cancellationToken = default)
    {
        var existing = await _categoryRepo.FindByNameAsync(createDto.Name, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException($"Category with name '{createDto.Name}' already exists.");

        var category = _mapper.Map<Category>(createDto);
        category.Id = Guid.NewGuid();
        category.CreatedAt = DateTime.UtcNow;

        await _categoryRepo.AddAsync(category, cancellationToken);
        _logger.LogInformation("Category {CategoryId} created", category.Id);
        return _mapper.Map<CategoryResponseDto>(category);
    }

    public async Task UpdateCategoryAsync(Guid id, UpdateCategoryDto updateDto, CancellationToken cancellationToken = default)
    {
        var existing = await _categoryRepo.GetByIdAsync(id, cancellationToken);
        if (existing == null)
            throw new KeyNotFoundException($"Category with id {id} not found");

        if (updateDto.Name != null && updateDto.Name != existing.Name)
        {
            var nameExists = await _categoryRepo.FindByNameAsync(updateDto.Name, cancellationToken);
            if (nameExists != null)
                throw new InvalidOperationException($"Category with name '{updateDto.Name}' already exists.");
        }

        _mapper.Map(updateDto, existing);
        await _categoryRepo.UpdateAsync(existing, cancellationToken);
        _logger.LogInformation("Category {CategoryId} updated", id);
    }

    public async Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _categoryRepo.GetByIdAsync(id, cancellationToken);
        if (existing == null)
            throw new KeyNotFoundException($"Category with id {id} not found");

        var documents = await _documentRepo.GetByCategoryAsync(id, cancellationToken);
        if (documents.Any())
            throw new InvalidOperationException("Cannot delete category with existing documents.");

        await _categoryRepo.DeleteAsync(id, cancellationToken);
        _logger.LogInformation("Category {CategoryId} deleted", id);
    }

    public async Task<PagedResult<DocumentListItemDto>> GetCategoryDocumentsAsync(
        Guid categoryId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepo.GetByIdAsync(categoryId, cancellationToken);
        if (category == null)
            throw new KeyNotFoundException($"Category with id {categoryId} not found");

        var documents = await _documentRepo.GetByCategoryAsync(categoryId, cancellationToken);
        var enumerable = documents as Document[] ?? documents.ToArray();
        var totalCount = enumerable.Count();
        var items = enumerable
            .OrderBy(d => d.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var dtos = _mapper.Map<List<DocumentListItemDto>>(items);
        return new PagedResult<DocumentListItemDto>
        {
            Items = dtos,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}