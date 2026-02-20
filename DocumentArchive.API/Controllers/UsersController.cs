using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.DTOs.Statistics;
using DocumentArchive.Core.DTOs.User;
using DocumentArchive.Core.Interfaces.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentArchive.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IValidator<CreateUserDto> _createValidator;
    private readonly IValidator<UpdateUserDto> _updateValidator;
    private readonly IUserService _userService;

    public UsersController(
        IUserService userService,
        IValidator<CreateUserDto> createValidator,
        IValidator<UpdateUserDto> updateValidator)
    {
        _userService = userService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    ///     Получает список пользователей с пагинацией и поиском
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<UserListItemDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
            return BadRequest("Page must be >= 1.");
        if (pageSize < 1 || pageSize > 100)
            return BadRequest("Page size must be between 1 and 100.");

        var result = await _userService.GetUsersAsync(page, pageSize, search, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    ///     Получает пользователя по ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserResponseDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userService.GetUserByIdAsync(id, cancellationToken);
        if (user == null)
            return NotFound();
        return Ok(user);
    }

    /// <summary>
    ///     Создаёт нового пользователя
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserResponseDto>> Create([FromBody] CreateUserDto createDto,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _createValidator.ValidateAsync(createDto, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var result = await _userService.CreateUserAsync(createDto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    ///     Обновляет пользователя
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto updateDto,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _updateValidator.ValidateAsync(updateDto, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        await _userService.UpdateUserAsync(id, updateDto, cancellationToken);
        return NoContent();
    }

    /// <summary>
    ///     Удаляет пользователя
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        await _userService.DeleteUserAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    ///     Получает документы пользователя с пагинацией
    /// </summary>
    [HttpGet("{id}/documents")]
    [ProducesResponseType(typeof(PagedResult<DocumentListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<DocumentListItemDto>>> GetUserDocuments(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
            return BadRequest("Page must be >= 1.");
        if (pageSize < 1 || pageSize > 100)
            return BadRequest("Page size must be between 1 and 100.");

        var result = await _userService.GetUserDocumentsAsync(id, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    ///     Получает общую статистику по пользователям
    /// </summary>
    [HttpGet("statistics/general")]
    [ProducesResponseType(typeof(UsersGeneralStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UsersGeneralStatisticsDto>> GetUsersGeneralStatistics(
        CancellationToken cancellationToken)
    {
        var result = await _userService.GetUsersGeneralStatisticsAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    ///     Получает статистику конкретного пользователя
    /// </summary>
    [HttpGet("{id}/statistics")]
    [ProducesResponseType(typeof(UserStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserStatisticsDto>> GetUserStatistics(Guid id, CancellationToken cancellationToken)
    {
        var result = await _userService.GetUserStatisticsAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    ///     Назначает роль пользователю
    /// </summary>
    [HttpPost("{userId}/roles/{roleId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AssignRole(Guid userId, Guid roleId, CancellationToken cancellationToken)
    {
        await _userService.AssignRoleAsync(userId, roleId, cancellationToken);
        return Ok();
    }

    /// <summary>
    ///     Удаляет роль у пользователя
    /// </summary>
    [HttpDelete("{userId}/roles/{roleId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveRole(Guid userId, Guid roleId, CancellationToken cancellationToken)
    {
        await _userService.RemoveRoleAsync(userId, roleId, cancellationToken);
        return NoContent();
    }
}