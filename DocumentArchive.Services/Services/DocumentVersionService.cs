using AutoMapper;
using DocumentArchive.Core.DTOs.DocumentVersion;
using DocumentArchive.Core.Interfaces.Services;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocumentArchive.Services.Services;

public class DocumentVersionService : IDocumentVersionService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<DocumentVersionService> _logger;

    public DocumentVersionService(AppDbContext context, IMapper mapper, ILogger<DocumentVersionService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    #region Private helper methods for permission checks

    private async Task<bool> CanAccessDocumentAsync(Guid documentId, Guid currentUserId, List<string> permissions, string requiredPermission = "ViewAnyDocument")
    {
        var document = await _context.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId);
        if (document == null)
            return false;

        return document.UserId == currentUserId || permissions.Contains(requiredPermission);
    }

    private async Task<bool> CanEditDocumentAsync(Guid documentId, Guid currentUserId, List<string> permissions)
    {
        var document = await _context.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId);
        if (document == null)
            return false;

        return (document.UserId == currentUserId && permissions.Contains("EditOwnDocuments"))
               || permissions.Contains("EditAnyDocument");
    }

    private async Task<bool> CanDeleteDocumentAsync(Guid documentId, Guid currentUserId, List<string> permissions)
    {
        var document = await _context.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId);
        if (document == null)
            return false;

        return (document.UserId == currentUserId && permissions.Contains("DeleteOwnDocuments"))
               || permissions.Contains("DeleteAnyDocument");
    }

    #endregion

    public async Task<List<DocumentVersionListItemDto>> GetAllAsync(Guid? documentId, Guid currentUserId, List<string> permissions, CancellationToken cancellationToken = default)
    {
        var query = _context.DocumentVersions
            .AsNoTracking()
            .AsQueryable();

        if (documentId.HasValue)
        {
            // Проверяем доступ к документу
            if (!await CanAccessDocumentAsync(documentId.Value, currentUserId, permissions))
                throw new UnauthorizedAccessException("You do not have permission to view versions of this document");

            query = query.Where(v => v.DocumentId == documentId.Value);
        }
        else
        {
            // Если documentId не указан, возвращаем только те версии, к документам которых есть доступ
            // Получаем все доступные документы
            var accessibleDocumentIds = await _context.Documents
                .Where(d => d.UserId == currentUserId || permissions.Contains("ViewAnyDocument"))
                .Select(d => d.Id)
                .ToListAsync(cancellationToken);

            query = query.Where(v => accessibleDocumentIds.Contains(v.DocumentId));
        }

        var versions = await query
            .OrderByDescending(v => v.UploadedAt)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<DocumentVersionListItemDto>>(versions);
    }

    public async Task<DocumentVersionResponseDto?> GetByIdAsync(Guid id, Guid currentUserId, List<string> permissions, CancellationToken cancellationToken)
    {
        var version = await _context.DocumentVersions
            .AsNoTracking()
            .Include(v => v.Document)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (version == null)
            return null;

        // Проверяем доступ к документу
        if (!await CanAccessDocumentAsync(version.DocumentId, currentUserId, permissions))
            return null;

        return _mapper.Map<DocumentVersionResponseDto>(version);
    }

    // Если мы решили добавить permissions в интерфейс, то метод будет выглядеть так:
    public async Task<DocumentVersionResponseDto> CreateAsync(CreateDocumentVersionDto dto, Guid currentUserId, List<string> permissions, CancellationToken cancellationToken)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == dto.DocumentId, cancellationToken);
        if (document == null)
            throw new InvalidOperationException($"Document with id {dto.DocumentId} not found.");

        // Проверка прав: владелец с правом EditOwnDocuments или любое с EditAnyDocument
        if (!(document.UserId == currentUserId && permissions.Contains("EditOwnDocuments"))
            && !permissions.Contains("EditAnyDocument"))
        {
            throw new UnauthorizedAccessException("You do not have permission to add versions to this document");
        }

        var versionExists = await _context.DocumentVersions
            .AnyAsync(v => v.DocumentId == dto.DocumentId && v.VersionNumber == dto.VersionNumber, cancellationToken);
        if (versionExists)
            throw new InvalidOperationException($"Version number {dto.VersionNumber} already exists for this document.");

        var version = _mapper.Map<DocumentVersion>(dto);
        version.Id = Guid.NewGuid();
        version.UploadedAt = DateTime.UtcNow;
        version.UploadedBy = currentUserId;

        _context.DocumentVersions.Add(version);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Document version {VersionId} created for document {DocumentId} by user {UserId}", version.Id, dto.DocumentId, currentUserId);
        return _mapper.Map<DocumentVersionResponseDto>(version);
    }

    public async Task UpdateAsync(Guid id, UpdateDocumentVersionDto dto, Guid currentUserId, List<string> permissions, CancellationToken cancellationToken)
    {
        var version = await _context.DocumentVersions
            .Include(v => v.Document)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (version == null)
            throw new KeyNotFoundException($"Document version with id {id} not found.");

        // Проверка прав: владелец документа с правом EditOwnDocuments или админ с EditAnyDocument
        if (!(version.Document.UserId == currentUserId && permissions.Contains("EditOwnDocuments"))
            && !permissions.Contains("EditAnyDocument"))
        {
            throw new UnauthorizedAccessException("You do not have permission to update this version");
        }

        // Обновляем только комментарий
        if (dto.Comment != null)
            version.Comment = dto.Comment;
        version.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Document version {VersionId} updated by user {UserId}", id, currentUserId);
    }

    public async Task DeleteAsync(Guid id, Guid currentUserId, List<string> permissions, CancellationToken cancellationToken)
    {
        var version = await _context.DocumentVersions
            .Include(v => v.Document)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (version == null)
            throw new KeyNotFoundException($"Document version with id {id} not found.");

        // Проверка прав: владелец документа с правом DeleteOwnDocuments или админ с DeleteAnyDocument
        if (!(version.Document.UserId == currentUserId && permissions.Contains("DeleteOwnDocuments"))
            && !permissions.Contains("DeleteAnyDocument"))
        {
            throw new UnauthorizedAccessException("You do not have permission to delete this version");
        }

        _context.DocumentVersions.Remove(version);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Document version {VersionId} deleted by user {UserId}", id, currentUserId);
    }
}