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
    /// Получает список документов с пагинацией, фильтрацией, поиском и сортировкой
    /// </summary>
    /// <param name="page">Номер страницы (по умолч. 1)</param>
    /// <param name="pageSize">Размер страницы (по умолч. 10)</param>
    /// <param name="search">Поисковый запрос (по названию и описанию)</param>
    /// <param name="categoryId">Фильтр по категории</param>
    /// <param name="userId">Фильтр по пользователю</param>
    /// <param name="fromDate">Фильтр по дате загрузки (от)</param>
    /// <param name="toDate">Фильтр по дате загрузки (до)</param>
    /// <param name="sortBy">Поле для сортировки (title, uploadDate)</param>
    /// <param name="sortOrder">Направление сортировки (asc, desc)</param>
    /// <returns>Страница с документами</returns>
    /// <response code="200">Успешно возвращён список</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DocumentListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DocumentListItemDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? sortBy = "uploadDate",
        [FromQuery] string? sortOrder = "desc")
    {
        var documents = await _documentRepo.GetAllAsync();

        // Фильтрация
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLowerInvariant();
            documents = documents.Where(d =>
                d.Title.ToLowerInvariant().Contains(search) ||
                (d.Description?.ToLowerInvariant().Contains(search) ?? false));
        }

        if (categoryId.HasValue)
            documents = documents.Where(d => d.CategoryId == categoryId);
        if (userId.HasValue)
            documents = documents.Where(d => d.UserId == userId);
        if (fromDate.HasValue)
            documents = documents.Where(d => d.UploadDate >= fromDate.Value);
        if (toDate.HasValue)
            documents = documents.Where(d => d.UploadDate <= toDate.Value);

        // Сортировка
        documents = sortBy?.ToLowerInvariant() switch
        {
            "title" => sortOrder == "asc"
                ? documents.OrderBy(d => d.Title)
                : documents.OrderByDescending(d => d.Title),
            "uploaddate" => sortOrder == "asc"
                ? documents.OrderBy(d => d.UploadDate)
                : documents.OrderByDescending(d => d.UploadDate),
            _ => documents.OrderByDescending(d => d.UploadDate)
        };

        // Пагинация
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

    /// <summary>
    /// Получает документ по уникальному идентификатору
    /// </summary>
    /// <param name="id">GUID документа</param>
    /// <returns>Полная информация о документе</returns>
    /// <response code="200">Документ найден</response>
    /// <response code="404">Документ не существует</response>
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
    /// <param name="createDto">Данные для создания документа</param>
    /// <returns>Созданный документ</returns>
    /// <response code="201">Документ успешно создан</response>
    /// <response code="400">Ошибка валидации или не найдена связанная сущность</response>
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
    /// <param name="id">GUID документа</param>
    /// <param name="updateDto">Новые данные документа</param>
    /// <returns>Нет содержимого</returns>
    /// <response code="204">Обновление выполнено успешно</response>
    /// <response code="404">Документ не найден</response>
    /// <response code="400">Указанная категория не существует</response>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDocumentDto updateDto)
    {
        var existing = await _documentRepo.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        // Проверка категории
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
    /// <param name="id">GUID документа</param>
    /// <returns>Нет содержимого</returns>
    /// <response code="204">Удаление выполнено успешно</response>
    /// <response code="404">Документ не найден</response>
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
    /// Массовое создание документов
    /// </summary>
    /// <param name="createDtos">Список документов для создания</param>
    /// <returns>Количество созданных документов</returns>
    /// <response code="200">Успешно созданы</response>
    [HttpPost("bulk")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateBulk([FromBody] List<CreateDocumentDto> createDtos)
    {
        var documents = _mapper.Map<List<Document>>(createDtos);
        foreach (var doc in documents)
        {
            doc.Id = Guid.NewGuid();
            doc.UploadDate = DateTime.UtcNow;
            await _documentRepo.AddAsync(doc);
        }
        return Ok(new { count = documents.Count });
    }

    /// <summary>
    /// Массовое удаление документов
    /// </summary>
    /// <param name="ids">Строка с GUID через запятую</param>
    /// <returns>Нет содержимого</returns>
    /// <response code="204">Удаление выполнено</response>
    [HttpDelete("bulk")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteBulk([FromQuery] string ids)
    {
        var guidList = ids.Split(',').Select(id => Guid.Parse(id.Trim())).ToList();
        foreach (var id in guidList)
        {
            await _documentRepo.DeleteAsync(id);
        }
        return NoContent();
    }

    /// <summary>
    /// Получает историю операций с документом
    /// </summary>
    /// <param name="id">GUID документа</param>
    /// <returns>Список логов</returns>
    /// <response code="200">Успешно</response>
    /// <response code="404">Документ не найден</response>
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