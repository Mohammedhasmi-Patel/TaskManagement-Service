using System;
using System.Collections.Generic;
using System.Text;

namespace TaskManagement.Domain.Entities;

public class TaskTimeEntry
{
    public Guid Id { get; private set; }
    public Guid TaskId { get; private set; }
    public DateTimeOffset StartTime { get; private set; }
    public DateTimeOffset? EndTime { get; private set; }
    public long DurationSeconds { get; private set; } = 0;

    public string? Description { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public Task Task { get; private set; } = null!;
}
