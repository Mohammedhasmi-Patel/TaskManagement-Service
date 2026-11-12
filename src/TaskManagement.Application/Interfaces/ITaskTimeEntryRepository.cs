using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Interfaces;

public interface ITaskTimeEntryRepository
{
    Task<TaskTimeEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TaskTimeEntry?> GetByIdAndUserIdAsync(Guid id, Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskTimeEntry>> GetAllByUserIdAsync(Guid userId, Guid? taskId = null, CancellationToken cancellationToken = default);
    Task AddAsync(TaskTimeEntry entry, CancellationToken cancellationToken = default);
    Task DeleteAsync(TaskTimeEntry entry, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

