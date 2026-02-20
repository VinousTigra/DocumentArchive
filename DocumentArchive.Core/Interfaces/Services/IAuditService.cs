using DocumentArchive.Core.Models;

namespace DocumentArchive.Core.Interfaces.Services;

public interface IAuditService
{
    Task LogAsync(SecurityEventType eventType, Guid? userId, string? userEmail, string? ipAddress, string? userAgent,
        bool success, object? details = null);
}