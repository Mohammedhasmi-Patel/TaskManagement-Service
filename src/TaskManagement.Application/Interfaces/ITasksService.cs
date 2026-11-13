using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Interfaces;

public interface ITasksService
{
    Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<TaskResponse>> GetAllAsync(
        Guid? projectId = null,
        TaskStatusEnum? status = null,
        TaskPriority? priority = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default);
    Task<TaskResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TaskResponse> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

