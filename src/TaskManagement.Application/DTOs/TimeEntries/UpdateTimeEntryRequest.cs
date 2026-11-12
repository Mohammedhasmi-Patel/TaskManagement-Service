namespace TaskManagement.Application.DTOs.TimeEntries;

public class UpdateTimeEntryRequest
{
    public Guid TaskId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public long? DurationSeconds { get; set; }
    public string? Description { get; set; }
}

