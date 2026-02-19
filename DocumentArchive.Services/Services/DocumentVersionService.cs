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
    private readonly ILogger<DocumentVersionService> _logger;
    private readonly IMapper _mapper;

    public DocumentVersionService(AppDbContext context, IMapper mapper, ILogger<DocumentVersionService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<DocumentVersionListItemDto>> GetAllAsync(Guid? documentId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DocumentVersions
            .AsNoTracking()
            .AsQueryable();

        if (documentId.HasValue)
            query = query.Where(v => v.DocumentId == documentId.Value);

        var versions = await query
            .OrderByDescending(v => v.UploadedAt)
            .ToListAsync(cancellationToken);
        return _mapper.Map<List<DocumentVersionListItemDto>>(versions);
    }

    public async Task<DocumentVersionResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var version = await _context.DocumentVersions
            .AsNoTracking()
            .Include(v => v.Document)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        return version == null ? null : _mapper.Map<DocumentVersionResponseDto>(version);
    }

    public async Task<DocumentVersionResponseDto> CreateAsync(CreateDocumentVersionDto dto,
        CancellationToken cancellationToken)
    {
        // Проверка существования документа
        var documentExists = await _context.Documents.AnyAsync(d => d.Id == dto.DocumentId, cancellationToken);
        if (!documentExists)
            throw new InvalidOperationException($"Document with id {dto.DocumentId} not found.");

        // Проверка уникальности номера версии для данного документа
        var versionExists = await _context.DocumentVersions
            .AnyAsync(v => v.DocumentId == dto.DocumentId && v.VersionNumber == dto.VersionNumber, cancellationToken);
        if (versionExists)
            throw new InvalidOperationException(
                $"Version number {dto.VersionNumber} already exists for this document.");

        var version = _mapper.Map<DocumentVersion>(dto);
        version.Id = Guid.NewGuid();
        version.UploadedAt = DateTime.UtcNow;

        _context.DocumentVersions.Add(version);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Document version {VersionId} created for document {DocumentId}", version.Id,
            dto.DocumentId);
        return _mapper.Map<DocumentVersionResponseDto>(version);
    }

    public async Task UpdateAsync(Guid id, UpdateDocumentVersionDto dto, CancellationToken cancellationToken)
    {
        var version = await _context.DocumentVersions.FindAsync(new object[] { id }, cancellationToken);
        if (version == null)
            throw new KeyNotFoundException($"Document version with id {id} not found.");

        // Обновляем только комментарий (остальные поля неизменяемы)
        if (dto.Comment != null)
            version.Comment = dto.Comment;
        version.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Document version {VersionId} updated", id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var version = await _context.DocumentVersions.FindAsync(new object[] { id }, cancellationToken);
        if (version == null)
            throw new KeyNotFoundException($"Document version with id {id} not found.");

        _context.DocumentVersions.Remove(version);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Document version {VersionId} deleted", id);
    }
}