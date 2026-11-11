using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Entities;

public class TaskTechnology : IAuditableEntity
{
    public Guid TaskId { get; private set; }
    public Guid TechnologyId { get; private set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public Task Task { get; private set; } = null!;
    public Technology Technology { get; private set; } = null!;

    private TaskTechnology()
    {
    }

    public static TaskTechnology Create(Guid taskId, Guid technologyId)
    {
        return new TaskTechnology
        {
            TaskId = taskId,
            TechnologyId = technologyId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
