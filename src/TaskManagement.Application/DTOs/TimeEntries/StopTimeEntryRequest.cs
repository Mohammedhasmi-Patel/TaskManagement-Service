namespace TaskManagement.Application.DTOs.TimeEntries;

public class StopTimeEntryRequest
{
    public DateTimeOffset? EndTime { get; set; }
    public string? Description { get; set; }
}
