using TaskManagement.Domain.Common;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Entities;

public class TaskItem : BaseEntity, IAuditableEntity
{
    public Guid ProjectId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public TaskStatusEnum Status { get; private set; } = TaskStatusEnum.Todo;
    public TaskPriority Priority { get; private set; } = TaskPriority.Medium;

    public decimal? EstimatedHours { get; private set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public Project Project { get; private set; } = null!;

    public ICollection<TaskTimeEntry> TimeEntries { get; private set; } = [];
    public ICollection<TaskTechnology> TaskTechnologies { get; private set; } = [];

    // Required by EF Core
    private TaskItem()
    {
    }

    public static TaskItem Create(
        Guid projectId,
        string title,
        string? description = null,
        TaskStatusEnum status = TaskStatusEnum.Todo,
        TaskPriority priority = TaskPriority.Medium,
        decimal? estimatedHours = null)
    {
        return new TaskItem
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Title = title.Trim(),
            Description = description?.Trim(),
            Status = status,
            Priority = priority,
            EstimatedHours = estimatedHours,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateDetails(
        string title,
        string? description = null,
        decimal? estimatedHours = null)
    {
        Title = title.Trim();
        Description = description?.Trim();
        EstimatedHours = estimatedHours;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateStatus(TaskStatusEnum status)
    {
        Status = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdatePriority(TaskPriority priority)
    {
        Priority = priority;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
