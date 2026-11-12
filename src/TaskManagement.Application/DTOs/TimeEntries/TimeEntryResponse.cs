namespace TaskManagement.Application.DTOs.TimeEntries;

public record TimeEntryResponse(
    Guid Id,
    Guid TaskId,
    DateTimeOffset StartTime,
    DateTimeOffset? EndTime,
    long DurationSeconds,
    string? Description,
    bool IsRunning,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);

