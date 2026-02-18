using AutoMapper;
using AutoMapper.QueryableExtensions;
using DocumentArchive.Core.DTOs.Category;
using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.Interfaces.Services;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocumentArchive.Services.Services;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(
        AppDbContext context,
        IMapper mapper,
        ILogger<CategoryService> logger)
    {
        _context = context;
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
        _logger.LogInformation("Getting categories page {Page} size {PageSize}", page, pageSize);

        var query = _context.Categories
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.Contains(search) ||
                                     (c.Description != null && c.Description.Contains(search)));

        // Сортировка
        query = (sortBy?.ToLower(), sortOrder?.ToLower()) switch
        {
            ("name", "desc") => query.OrderByDescending(c => c.Name),
            ("name", _) => query.OrderBy(c => c.Name),
            ("createdat", "desc") => query.OrderByDescending(c => c.CreatedAt),
            ("createdat", _) => query.OrderBy(c => c.CreatedAt),
            _ => query.OrderBy(c => c.Name)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ProjectTo<CategoryListItemDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new PagedResult<CategoryListItemDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<CategoryResponseDto?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return category == null ? null : _mapper.Map<CategoryResponseDto>(category);
    }

    public async Task<CategoryResponseDto> CreateCategoryAsync(CreateCategoryDto createDto, CancellationToken cancellationToken = default)
    {
        // Проверка уникальности имени
        var nameExists = await _context.Categories
            .AnyAsync(c => c.Name == createDto.Name, cancellationToken);
        if (nameExists)
            throw new InvalidOperationException($"Category with name '{createDto.Name}' already exists.");

        var category = _mapper.Map<Category>(createDto);
        category.Id = Guid.NewGuid();
        category.CreatedAt = DateTime.UtcNow;

        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Category {CategoryId} created", category.Id);
        return _mapper.Map<CategoryResponseDto>(category);
    }

    public async Task UpdateCategoryAsync(Guid id, UpdateCategoryDto updateDto, CancellationToken cancellationToken = default)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category == null)
            throw new KeyNotFoundException($"Category with id {id} not found");

        if (!string.IsNullOrWhiteSpace(updateDto.Name) && updateDto.Name != category.Name)
        {
            var nameExists = await _context.Categories
                .AnyAsync(c => c.Name == updateDto.Name && c.Id != id, cancellationToken);
            if (nameExists)
                throw new InvalidOperationException($"Category with name '{updateDto.Name}' already exists.");
        }

        _mapper.Map(updateDto, category);
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Category {CategoryId} updated", id);
    }

    public async Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _context.Categories
            .Include(c => c.Documents)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category == null)
            throw new KeyNotFoundException($"Category with id {id} not found");

        if (category.Documents.Any())
            throw new InvalidOperationException("Cannot delete category with existing documents.");

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Category {CategoryId} deleted", id);
    }

    public async Task<PagedResult<DocumentListItemDto>> GetCategoryDocumentsAsync(
        Guid categoryId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var categoryExists = await _context.Categories
            .AnyAsync(c => c.Id == categoryId, cancellationToken);
        if (!categoryExists)
            throw new KeyNotFoundException($"Category with id {categoryId} not found");

        var query = _context.Documents
            .AsNoTracking()
            .Where(d => d.CategoryId == categoryId)
            .OrderBy(d => d.Title);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ProjectTo<DocumentListItemDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new PagedResult<DocumentListItemDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}