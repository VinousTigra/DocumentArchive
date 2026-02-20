namespace DocumentArchive.Core.Models;

public enum SecurityEventType
{
    Login,
    FailedLogin,
    Logout,
    Register,
    PasswordChange,
    PasswordReset,
    PasswordResetRequested,
    EmailConfirmed,
    RoleAssigned,
    RoleRevoked,
    TokenRefresh,
    TokenRevoke
}

public class SecurityAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public SecurityEventType EventType { get; set; }
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool Success { get; set; }
    public string? Details { get; set; } // JSON для дополнительной информации
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}