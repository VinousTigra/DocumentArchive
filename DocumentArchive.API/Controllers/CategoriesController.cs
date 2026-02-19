using DocumentArchive.Core.DTOs.Category;
using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.DTOs.Statistics;
using DocumentArchive.Core.Interfaces.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace DocumentArchive.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly IValidator<CreateCategoryDto> _createValidator;
    private readonly IValidator<UpdateCategoryDto> _updateValidator;

    public CategoriesController(
        ICategoryService categoryService,
        IValidator<CreateCategoryDto> createValidator,
        IValidator<UpdateCategoryDto> updateValidator)
    {
        _categoryService = categoryService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    ///     Получает список категорий с пагинацией, поиском и сортировкой
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CategoryListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<CategoryListItemDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = "name",
        [FromQuery] string? sortOrder = "asc",
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
            return BadRequest("Page must be >= 1.");
        if (pageSize < 1 || pageSize > 100)
            return BadRequest("Page size must be between 1 and 100.");
        if (sortOrder?.ToLower() != "asc" && sortOrder?.ToLower() != "desc")
            return BadRequest("Sort order must be 'asc' or 'desc'.");

        var allowedSortFields = new[] { "name", "createdat" };
        if (!string.IsNullOrWhiteSpace(sortBy) && !allowedSortFields.Contains(sortBy.ToLowerInvariant()))
            return BadRequest($"Invalid sort field. Allowed values: {string.Join(", ", allowedSortFields)}.");

        var result =
            await _categoryService.GetCategoriesAsync(page, pageSize, search, sortBy, sortOrder, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    ///     Получает категорию по ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CategoryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CategoryResponseDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryService.GetCategoryByIdAsync(id, cancellationToken);
        if (category == null)
            return NotFound();
        return Ok(category);
    }

    /// <summary>
    ///     Создаёт новую категорию
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CategoryResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CategoryResponseDto>> Create([FromBody] CreateCategoryDto createDto,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _createValidator.ValidateAsync(createDto, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var result = await _categoryService.CreateCategoryAsync(createDto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    ///     Обновляет категорию
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryDto updateDto,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _updateValidator.ValidateAsync(updateDto, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        await _categoryService.UpdateCategoryAsync(id, updateDto, cancellationToken);
        return NoContent();
    }

    /// <summary>
    ///     Удаляет категорию
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        await _categoryService.DeleteCategoryAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    ///     Получает документы в категории с пагинацией
    /// </summary>
    [HttpGet("{id}/documents")]
    [ProducesResponseType(typeof(PagedResult<DocumentListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<DocumentListItemDto>>> GetDocumentsByCategory(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
            return BadRequest("Page must be >= 1.");
        if (pageSize < 1 || pageSize > 100)
            return BadRequest("Page size must be between 1 and 100.");

        var result = await _categoryService.GetCategoryDocumentsAsync(id, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    ///     Получает список категорий с количеством документов в каждой
    /// </summary>
    [HttpGet("with-document-count")]
    [ProducesResponseType(typeof(List<CategoryWithDocumentCountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<CategoryWithDocumentCountDto>>> GetCategoriesWithDocumentCount(
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetCategoriesWithDocumentCountAsync(cancellationToken);
        return Ok(result);
    }
}