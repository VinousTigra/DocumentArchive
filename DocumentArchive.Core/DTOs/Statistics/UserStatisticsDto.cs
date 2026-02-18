namespace DocumentArchive.Core.DTOs.Statistics;

public class UserStatisticsDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int DocumentsCount { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime RegisteredAt { get; set; }
}