using DocumentArchive.Core.Models;

namespace DocumentArchive.Core.DTOs.Statistics;

public class LogsStatisticsDto
{
    public int TotalLogs { get; set; }
    public int CriticalLogs { get; set; }
    public List<ActionTypeCountDto> LogsByActionType { get; set; } = new();
}

public class ActionTypeCountDto
{
    public ActionType ActionType { get; set; }
    public int Count { get; set; }
}