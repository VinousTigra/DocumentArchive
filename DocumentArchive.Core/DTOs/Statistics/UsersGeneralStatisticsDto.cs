namespace DocumentArchive.Core.DTOs.Statistics;

public class UsersGeneralStatisticsDto
{
    public int TotalUsers { get; set; }
    public int ActiveToday { get; set; }
    public List<DateCountDto> UsersByRegistrationDate { get; set; } = new();
}

public class DateCountDto
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}