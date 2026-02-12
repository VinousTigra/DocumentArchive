using AutoMapper;
using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.DTOs.User;
using DocumentArchive.Core.Interfaces;    // <-- Добавить!
using DocumentArchive.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace DocumentArchive.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepo;          // <-- Интерфейс
    private readonly IDocumentRepository _documentRepo;  // <-- Интерфейс
    private readonly IMapper _mapper;

    public UsersController(
        IUserRepository userRepo,
        IDocumentRepository documentRepo,
        IMapper mapper)
    {
        _userRepo = userRepo;
        _documentRepo = documentRepo;
        _mapper = mapper;
    }

    /// <summary>
    /// Получает список пользователей с пагинацией и поиском
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UserListItemDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var users = await _userRepo.GetAllAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLowerInvariant();
            users = users.Where(u =>
                u.Username.ToLowerInvariant().Contains(search) ||
                u.Email.ToLowerInvariant().Contains(search));
        }

        users = users.OrderBy(u => u.Username);

        var totalCount = users.Count();
        var items = users
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var dtos = _mapper.Map<List<UserListItemDto>>(items);
        var result = new PagedResult<UserListItemDto>
        {
            Items = dtos,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Ok(result);
    }

    /// <summary>
    /// Получает пользователя по ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponseDto>> GetById(Guid id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null)
            return NotFound();

        var dto = _mapper.Map<UserResponseDto>(user);
        return Ok(dto);
    }

    /// <summary>
    /// Создаёт нового пользователя
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserResponseDto>> Create([FromBody] CreateUserDto createDto)
    {
        var existing = await _userRepo.FindByEmailAsync(createDto.Email);
        if (existing != null)
            return BadRequest($"User with email '{createDto.Email}' already exists.");

        var user = _mapper.Map<User>(createDto);
        user.Id = Guid.NewGuid();
        user.CreatedAt = DateTime.UtcNow;

        await _userRepo.AddAsync(user);

        var dto = _mapper.Map<UserResponseDto>(user);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, dto);
    }

    /// <summary>
    /// Обновляет пользователя
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto updateDto)
    {
        var existing = await _userRepo.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        if (updateDto.Email != null && updateDto.Email != existing.Email)
        {
            var emailExists = await _userRepo.FindByEmailAsync(updateDto.Email);
            if (emailExists != null)
                return BadRequest($"User with email '{updateDto.Email}' already exists.");
        }

        _mapper.Map(updateDto, existing);
        existing.UpdatedAt = DateTime.UtcNow;

        await _userRepo.UpdateAsync(existing);
        return NoContent();
    }

    /// <summary>
    /// Удаляет пользователя
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await _userRepo.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        var documents = await _documentRepo.GetByUserAsync(id);
        if (documents.Any())
            return BadRequest("Cannot delete user with existing documents.");

        await _userRepo.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Получает документы пользователя
    /// </summary>
    [HttpGet("{id}/documents")]
    [ProducesResponseType(typeof(IEnumerable<DocumentListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<DocumentListItemDto>>> GetUserDocuments(Guid id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null)
            return NotFound();

        var documents = await _documentRepo.GetByUserAsync(id);
        var dtos = _mapper.Map<IEnumerable<DocumentListItemDto>>(documents);
        return Ok(dtos);
    }
}