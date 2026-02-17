using AutoMapper;
using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.DTOs.User;
using DocumentArchive.Core.Interfaces.Repositorys;
using DocumentArchive.Core.Interfaces.Services;
using DocumentArchive.Core.Models;
using Microsoft.Extensions.Logging;

namespace DocumentArchive.Services.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;
    private readonly IDocumentRepository _documentRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepo,
        IDocumentRepository documentRepo,
        IMapper mapper,
        ILogger<UserService> logger)
    {
        _userRepo = userRepo;
        _documentRepo = documentRepo;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResult<UserListItemDto>> GetUsersAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting users page {Page} size {PageSize}, search '{Search}'", page, pageSize, search);
        var paged = await _userRepo.GetPagedAsync(page, pageSize, search, cancellationToken);
        var dtos = _mapper.Map<List<UserListItemDto>>(paged.Items);
        return new PagedResult<UserListItemDto>
        {
            Items = dtos,
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount
        };
    }

    public async Task<UserResponseDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.GetByIdAsync(id, cancellationToken);
        return user == null ? null : _mapper.Map<UserResponseDto>(user);
    }

    public async Task<UserResponseDto> CreateUserAsync(CreateUserDto createDto, CancellationToken cancellationToken = default)
    {
        var existing = await _userRepo.FindByEmailAsync(createDto.Email, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException($"User with email '{createDto.Email}' already exists.");

        var user = _mapper.Map<User>(createDto);
        user.Id = Guid.NewGuid();
        user.CreatedAt = DateTime.UtcNow;

        await _userRepo.AddAsync(user, cancellationToken);
        _logger.LogInformation("User {UserId} created", user.Id);
        return _mapper.Map<UserResponseDto>(user);
    }

    public async Task UpdateUserAsync(Guid id, UpdateUserDto updateDto, CancellationToken cancellationToken = default)
    {
        var existing = await _userRepo.GetByIdAsync(id, cancellationToken);
        if (existing == null)
            throw new KeyNotFoundException($"User with id {id} not found");

        if (updateDto.Email != null && updateDto.Email != existing.Email)
        {
            var emailExists = await _userRepo.FindByEmailAsync(updateDto.Email, cancellationToken);
            if (emailExists != null)
                throw new InvalidOperationException($"User with email '{updateDto.Email}' already exists.");
        }

        _mapper.Map(updateDto, existing);
        existing.UpdatedAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(existing, cancellationToken);
        _logger.LogInformation("User {UserId} updated", id);
    }

    public async Task DeleteUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _userRepo.GetByIdAsync(id, cancellationToken);
        if (existing == null)
            throw new KeyNotFoundException($"User with id {id} not found");

        var documents = await _documentRepo.GetByUserAsync(id, cancellationToken);
        if (documents.Any())
            throw new InvalidOperationException("Cannot delete user with existing documents.");

        await _userRepo.DeleteAsync(id, cancellationToken);
        _logger.LogInformation("User {UserId} deleted", id);
    }

    public async Task<PagedResult<DocumentListItemDto>> GetUserDocumentsAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException($"User with id {userId} not found");

        var documents = await _documentRepo.GetByUserAsync(userId, cancellationToken);
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