using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Entities;

public class TaskTimeEntry : BaseEntity, IAuditableEntity
{
    public Guid TaskId { get; private set; }
    public DateTimeOffset StartTime { get; private set; }
    public DateTimeOffset? EndTime { get; private set; }
    public long DurationSeconds { get; private set; } = 0;

    public string? Description { get; private set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public TaskItem Task { get; private set; } = null!;

    private TaskTimeEntry()
    {
    }

    public static TaskTimeEntry Create(
        Guid taskId,
        DateTimeOffset startTime,
        DateTimeOffset? endTime = null,
        string? description = null,
        long? durationSeconds = null)
    {
        return new TaskTimeEntry
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            StartTime = startTime,
            EndTime = endTime,
            DurationSeconds = CalculateDurationSeconds(startTime, endTime, durationSeconds),
            Description = description?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateDetails(
        DateTimeOffset startTime,
        DateTimeOffset? endTime = null,
        string? description = null,
        long? durationSeconds = null)
    {
        StartTime = startTime;
        EndTime = endTime;
        DurationSeconds = CalculateDurationSeconds(startTime, endTime, durationSeconds);
        Description = description?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Stop(DateTimeOffset? endTime = null, string? description = null)
    {
        var effectiveEndTime = endTime ?? DateTimeOffset.UtcNow;
        EndTime = effectiveEndTime;
        DurationSeconds = CalculateDurationSeconds(StartTime, effectiveEndTime, null);
        if (description is not null)
        {
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        }
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static long CalculateDurationSeconds(
        DateTimeOffset startTime,
        DateTimeOffset? endTime,
        long? durationSeconds)
    {
        if (durationSeconds.HasValue)
        {
            return Math.Max(0, durationSeconds.Value);
        }

        if (endTime.HasValue && endTime.Value > startTime)
        {
            return (long)(endTime.Value - startTime).TotalSeconds;
        }

        return 0;
    }
}
