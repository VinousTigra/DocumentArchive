using AutoMapper;
using DocumentArchive.Core.DTOs.ArchiveLog;
using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace DocumentArchive.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class DocumentsController : ControllerBase
{
    private readonly DocumentRepository _documentRepo;
    private readonly CategoryRepository _categoryRepo;
    private readonly UserRepository _userRepo;
    private readonly ArchiveLogRepository _logRepo;
    private readonly IMapper _mapper;

    public DocumentsController(
        DocumentRepository documentRepo,
        CategoryRepository categoryRepo,
        UserRepository userRepo,
        ArchiveLogRepository logRepo,
        IMapper mapper)
    {
        _documentRepo = documentRepo;
        _categoryRepo = categoryRepo;
        _userRepo = userRepo;
        _logRepo = logRepo;
        _mapper = mapper;
    }

    /// <summary>
    /// Получает список документов с пагинацией, фильтрацией, поиском и множественной сортировкой
    /// </summary>
    /// <param name="page">Номер страницы (по умолч. 1)</param>
    /// <param name="pageSize">Размер страницы (по умолч. 10)</param>
    /// <param name="search">Поисковый запрос (по названию и описанию)</param>
    /// <param name="categoryIds">Фильтр по нескольким категориям (ID через запятую)</param>
    /// <param name="userId">Фильтр по пользователю</param>
    /// <param name="fromDate">Фильтр по дате загрузки (от)</param>
    /// <param name="toDate">Фильтр по дате загрузки (до)</param>
    /// <param name="sort">Поля для сортировки, например: title:asc,uploadDate:desc</param>
    /// <returns>Страница с документами</returns>
    /// <response code="200">Успешно возвращён список</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DocumentListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DocumentListItemDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? categoryIds = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? sort = null)
    {
        var documents = await _documentRepo.GetAllAsync();

        // ---- Фильтрация ----
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLowerInvariant();
            documents = documents.Where(d =>
                d.Title.ToLowerInvariant().Contains(search) ||
                (d.Description?.ToLowerInvariant().Contains(search) ?? false));
        }

        if (!string.IsNullOrWhiteSpace(categoryIds))
        {
            var ids = categoryIds.Split(',')
                .Select(id => Guid.TryParse(id.Trim(), out var g) ? g : (Guid?)null)
                .Where(g => g.HasValue)
                .Select(g => g.Value)
                .ToList();
            if (ids.Any())
                documents = documents.Where(d => d.CategoryId.HasValue && ids.Contains(d.CategoryId.Value));
        }

        if (userId.HasValue)
            documents = documents.Where(d => d.UserId == userId);
        if (fromDate.HasValue)
            documents = documents.Where(d => d.UploadDate >= fromDate.Value);
        if (toDate.HasValue)
            documents = documents.Where(d => d.UploadDate <= toDate.Value);

        // ---- Множественная сортировка ----
        if (!string.IsNullOrWhiteSpace(sort))
        {
            var sortFields = sort.Split(',');
            IOrderedEnumerable<Document>? ordered = null;

            foreach (var field in sortFields)
            {
                var parts = field.Split(':');
                var fieldName = parts[0].Trim().ToLowerInvariant();
                var direction = parts.Length > 1 ? parts[1].Trim().ToLowerInvariant() : "asc";

                if (ordered == null)
                {
                    ordered = direction == "asc"
                        ? documents.OrderBy(GetSortProperty(fieldName))
                        : documents.OrderByDescending(GetSortProperty(fieldName));
                }
                else
                {
                    ordered = direction == "asc"
                        ? ordered.ThenBy(GetSortProperty(fieldName))
                        : ordered.ThenByDescending(GetSortProperty(fieldName));
                }
            }

            if (ordered != null)
                documents = ordered;
        }
        else
        {
            documents = documents.OrderByDescending(d => d.UploadDate);
        }

        // ---- Пагинация ----
        var totalCount = documents.Count();
        var items = documents
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var dtos = _mapper.Map<List<DocumentListItemDto>>(items);
        var result = new PagedResult<DocumentListItemDto>
        {
            Items = dtos,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Ok(result);
    }

    private static Func<Document, object> GetSortProperty(string fieldName) => fieldName switch
    {
        "title" => d => d.Title,
        "uploaddate" => d => d.UploadDate,
        "filename" => d => d.FileName,
        _ => d => d.UploadDate
    };

    /// <summary>
    /// Получает документ по уникальному идентификатору
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DocumentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentResponseDto>> GetById(Guid id)
    {
        var document = await _documentRepo.GetByIdAsync(id);
        if (document == null)
            return NotFound();

        var dto = _mapper.Map<DocumentResponseDto>(document);
        return Ok(dto);
    }

    /// <summary>
    /// Создаёт новый документ
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(DocumentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DocumentResponseDto>> Create([FromBody] CreateDocumentDto createDto)
    {
        // Проверка существования категории
        if (createDto.CategoryId.HasValue)
        {
            var category = await _categoryRepo.GetByIdAsync(createDto.CategoryId.Value);
            if (category == null)
                return BadRequest($"Category with id {createDto.CategoryId} not found");
        }

        // Проверка существования пользователя
        if (createDto.UserId.HasValue)
        {
            var user = await _userRepo.GetByIdAsync(createDto.UserId.Value);
            if (user == null)
                return BadRequest($"User with id {createDto.UserId} not found");
        }

        var document = _mapper.Map<Document>(createDto);
        document.Id = Guid.NewGuid();
        document.UploadDate = DateTime.UtcNow;

        await _documentRepo.AddAsync(document);

        // Создаём запись в логе
        if (createDto.UserId.HasValue)
        {
            var log = new ArchiveLog
            {
                Id = Guid.NewGuid(),
                Action = "Created",
                ActionType = ActionType.Created,
                IsCritical = false,
                Timestamp = DateTime.UtcNow,
                UserId = createDto.UserId.Value,
                DocumentId = document.Id
            };
            await _logRepo.AddAsync(log);
        }

        var dto = _mapper.Map<DocumentResponseDto>(document);
        return CreatedAtAction(nameof(GetById), new { id = document.Id }, dto);
    }

    /// <summary>
    /// Полностью обновляет документ
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDocumentDto updateDto)
    {
        var existing = await _documentRepo.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        if (updateDto.CategoryId.HasValue)
        {
            var category = await _categoryRepo.GetByIdAsync(updateDto.CategoryId.Value);
            if (category == null)
                return BadRequest($"Category with id {updateDto.CategoryId} not found");
        }

        _mapper.Map(updateDto, existing);
        existing.UpdatedAt = DateTime.UtcNow;

        await _documentRepo.UpdateAsync(existing);
        return NoContent();
    }

    /// <summary>
    /// Удаляет документ
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await _documentRepo.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        await _documentRepo.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Массовое создание документов с детальным ответом
    /// </summary>
    [HttpPost("bulk")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateBulk([FromBody] List<CreateDocumentDto> createDtos)
    {
        var results = new List<object>();

        foreach (var dto in createDtos)
        {
            try
            {
                var document = _mapper.Map<Document>(dto);
                document.Id = Guid.NewGuid();
                document.UploadDate = DateTime.UtcNow;
                await _documentRepo.AddAsync(document);
                results.Add(new { id = document.Id, success = true });
            }
            catch (Exception ex)
            {
                results.Add(new { id = (Guid?)null, success = false, error = ex.Message });
            }
        }

        return Ok(new { count = results.Count, results });
    }

    /// <summary>
    /// Массовое удаление документов с детальным ответом
    /// </summary>
    [HttpDelete("bulk")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteBulk([FromQuery] string ids)
    {
        var guidList = ids.Split(',')
            .Select(id => Guid.TryParse(id.Trim(), out var g) ? g : (Guid?)null)
            .Where(g => g.HasValue)
            .Select(g => g.Value)
            .ToList();

        var results = new List<object>();

        foreach (var id in guidList)
        {
            try
            {
                await _documentRepo.DeleteAsync(id);
                results.Add(new { id, success = true });
            }
            catch (Exception ex)
            {
                results.Add(new { id, success = false, error = ex.Message });
            }
        }

        return Ok(new { count = results.Count, results });
    }

    /// <summary>
    /// Массовое обновление документов
    /// </summary>
    [HttpPut("bulk")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateBulk([FromBody] List<UpdateBulkDocumentDto> updateDtos)
    {
        var results = new List<object>();

        foreach (var dto in updateDtos)
        {
            try
            {
                var existing = await _documentRepo.GetByIdAsync(dto.Id);
                if (existing == null)
                {
                    results.Add(new { id = dto.Id, success = false, error = "Document not found" });
                    continue;
                }

                _mapper.Map(dto, existing);
                existing.UpdatedAt = DateTime.UtcNow;
                await _documentRepo.UpdateAsync(existing);
                results.Add(new { id = dto.Id, success = true });
            }
            catch (Exception ex)
            {
                results.Add(new { id = dto.Id, success = false, error = ex.Message });
            }
        }

        return Ok(new { count = results.Count, results });
    }

    /// <summary>
    /// Получает историю операций с документом
    /// </summary>
    [HttpGet("{id}/logs")]
    [ProducesResponseType(typeof(IEnumerable<ArchiveLogListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ArchiveLogListItemDto>>> GetDocumentLogs(Guid id)
    {
        var document = await _documentRepo.GetByIdAsync(id);
        if (document == null)
            return NotFound();

        var logs = await _logRepo.GetByDocumentIdAsync(id);
        var dtos = _mapper.Map<IEnumerable<ArchiveLogListItemDto>>(logs);
        return Ok(dtos);
    }
}