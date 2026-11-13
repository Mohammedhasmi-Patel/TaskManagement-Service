namespace TaskManagement.Application.DTOs.Tasks;

public record TaskTimeEntryResponse(
    string? Description,
    DateTimeOffset StartTime,
    DateTimeOffset? EndTime,
    long TotalTimeInSeconds
);

