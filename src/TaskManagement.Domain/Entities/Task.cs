using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Domain.Enums;


namespace TaskManagement.Domain.Entities;

public class Task
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public TaskStatusEnum Status { get; private set; } = TaskStatusEnum.Todo;
    public TaskPriority Priority { get; private set; } = TaskPriority.Medium;

    public decimal? EstimatedHours { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; private set; }

    // Navigation Properties
    public Project Project { get; private set; } = null!;

    public ICollection<TaskTimeEntry> TimeEntries { get; private set; } = new List<TaskTimeEntry>();
    public ICollection<TaskTechnology> TaskTechnologies { get; private set; } = new List<TaskTechnology>();

}
