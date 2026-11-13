using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Infrastructure.Persistence.Repositories;

public class TaskRepository(ApplicationDbContext dbContext) : ITaskRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tasks
            .Include(t => t.TimeEntries)
            .Include(t => t.TaskTechnologies)
                .ThenInclude(tt => tt.Technology)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<TaskItem?> GetByIdAndUserIdAsync(Guid id, Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges
            ? _dbContext.Tasks
            : _dbContext.Tasks.AsNoTracking();

        return await query
            .Include(t => t.TimeEntries)
            .Include(t => t.TaskTechnologies)
                .ThenInclude(tt => tt.Technology)
            .FirstOrDefaultAsync(t => t.Id == id && t.Project.Company.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<TaskItem>> GetAllByUserIdAsync(
        Guid userId,
        Guid? projectId = null,
        TaskStatusEnum? status = null,
        TaskPriority? priority = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var fromUtc = fromDate?.ToUniversalTime();
        var toUtc = toDate?.ToUniversalTime();

        return await _dbContext.Tasks
            .AsNoTracking()
            .Include(t => t.TaskTechnologies)
                .ThenInclude(tt => tt.Technology)
            .Where(t =>
                t.Project.Company.UserId == userId
                && (!projectId.HasValue || t.ProjectId == projectId.Value)
                && (!status.HasValue || t.Status == status.Value)
                && (!priority.HasValue || t.Priority == priority.Value)
                && (!fromUtc.HasValue || t.CreatedAt >= fromUtc.Value)
                && (!toUtc.HasValue || t.CreatedAt <= toUtc.Value))
            .OrderBy(t => t.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        await _dbContext.Tasks.AddAsync(task, cancellationToken);
    }

    public Task DeleteAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        _dbContext.Tasks.Remove(task);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

