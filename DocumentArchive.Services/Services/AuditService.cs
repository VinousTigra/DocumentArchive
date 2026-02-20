using System.Text.Json;
using DocumentArchive.Core.Interfaces.Services;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Data;

namespace DocumentArchive.Services.Services;

public class AuditService : IAuditService
{
    private readonly AppDbContext _context;

    public AuditService(AppDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(SecurityEventType eventType, Guid? userId, string? userEmail, string? ipAddress,
        string? userAgent, bool success, object? details = null)
    {
        var log = new SecurityAuditLog
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            UserId = userId,
            UserEmail = userEmail,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Success = success,
            Details = details != null ? JsonSerializer.Serialize(details) : null,
            Timestamp = DateTime.UtcNow
        };
        _context.SecurityAuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}