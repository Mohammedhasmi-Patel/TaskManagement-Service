using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Interfaces;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TaskItem?> GetByIdAndUserIdAsync(Guid id, Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskItem>> GetAllByUserIdAsync(
        Guid userId,
        Guid? projectId = null,
        TaskStatusEnum? status = null,
        TaskPriority? priority = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(TaskItem task, CancellationToken cancellationToken = default);
    Task DeleteAsync(TaskItem task, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

