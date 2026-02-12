using AutoMapper;
using DocumentArchive.Core.DTOs.Category;
using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.Interfaces;    // <-- Добавить!
using DocumentArchive.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace DocumentArchive.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryRepository _categoryRepo;   // <-- Интерфейс
    private readonly IDocumentRepository _documentRepo;   // <-- Интерфейс
    private readonly IMapper _mapper;

    public CategoriesController(
        ICategoryRepository categoryRepo,
        IDocumentRepository documentRepo,
        IMapper mapper)
    {
        _categoryRepo = categoryRepo;
        _documentRepo = documentRepo;
        _mapper = mapper;
    }

    /// <summary>
    /// Получает список категорий с пагинацией, поиском и сортировкой
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CategoryListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CategoryListItemDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = "name",
        [FromQuery] string? sortOrder = "asc")
    {
        var categories = await _categoryRepo.GetAllAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLowerInvariant();
            categories = categories.Where(c =>
                c.Name.ToLowerInvariant().Contains(search) ||
                (c.Description?.ToLowerInvariant().Contains(search) ?? false));
        }

        categories = sortBy?.ToLowerInvariant() switch
        {
            "name" => sortOrder == "asc"
                ? categories.OrderBy(c => c.Name)
                : categories.OrderByDescending(c => c.Name),
            "createdat" => sortOrder == "asc"
                ? categories.OrderBy(c => c.CreatedAt)
                : categories.OrderByDescending(c => c.CreatedAt),
            _ => categories.OrderBy(c => c.Name)
        };

        var totalCount = categories.Count();
        var items = categories
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var dtos = _mapper.Map<List<CategoryListItemDto>>(items);
        var result = new PagedResult<CategoryListItemDto>
        {
            Items = dtos,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Ok(result);
    }

    /// <summary>
    /// Получает категорию по ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CategoryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponseDto>> GetById(Guid id)
    {
        var category = await _categoryRepo.GetByIdAsync(id);
        if (category == null)
            return NotFound();

        var dto = _mapper.Map<CategoryResponseDto>(category);
        return Ok(dto);
    }

    /// <summary>
    /// Создаёт новую категорию
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CategoryResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CategoryResponseDto>> Create([FromBody] CreateCategoryDto createDto)
    {
        var existing = await _categoryRepo.FindByNameAsync(createDto.Name);
        if (existing != null)
            return BadRequest($"Category with name '{createDto.Name}' already exists.");

        var category = _mapper.Map<Category>(createDto);
        category.Id = Guid.NewGuid();
        category.CreatedAt = DateTime.UtcNow;

        await _categoryRepo.AddAsync(category);

        var dto = _mapper.Map<CategoryResponseDto>(category);
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, dto);
    }

    /// <summary>
    /// Обновляет категорию
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryDto updateDto)
    {
        var existing = await _categoryRepo.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        if (updateDto.Name != null && updateDto.Name != existing.Name)
        {
            var nameExists = await _categoryRepo.FindByNameAsync(updateDto.Name);
            if (nameExists != null)
                return BadRequest($"Category with name '{updateDto.Name}' already exists.");
        }

        _mapper.Map(updateDto, existing);
        await _categoryRepo.UpdateAsync(existing);

        return NoContent();
    }

    /// <summary>
    /// Удаляет категорию
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await _categoryRepo.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        var documents = await _documentRepo.GetByCategoryAsync(id);
        if (documents.Any())
            return BadRequest("Cannot delete category with existing documents.");

        await _categoryRepo.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Получает документы в категории
    /// </summary>
    [HttpGet("{id}/documents")]
    [ProducesResponseType(typeof(IEnumerable<DocumentListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<DocumentListItemDto>>> GetDocumentsByCategory(Guid id)
    {
        var category = await _categoryRepo.GetByIdAsync(id);
        if (category == null)
            return NotFound();

        var documents = await _documentRepo.GetByCategoryAsync(id);
        var dtos = _mapper.Map<IEnumerable<DocumentListItemDto>>(documents);
        return Ok(dtos);
    }
}