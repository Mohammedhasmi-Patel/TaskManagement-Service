using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Repositories;

public class TaskTimeEntryRepository(ApplicationDbContext dbContext) : ITaskTimeEntryRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<TaskTimeEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TaskTimeEntries
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<TaskTimeEntry?> GetByIdAndUserIdAsync(Guid id, Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges
            ? _dbContext.TaskTimeEntries
            : _dbContext.TaskTimeEntries.AsNoTracking();

        return await query
            .FirstOrDefaultAsync(e => e.Id == id && e.Task.Project.Company.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<TaskTimeEntry>> GetAllByUserIdAsync(Guid userId, Guid? taskId = null, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TaskTimeEntries
            .AsNoTracking()
            .Where(e => e.Task.Project.Company.UserId == userId && (!taskId.HasValue || e.TaskId == taskId.Value))
            .OrderByDescending(e => e.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TaskTimeEntry entry, CancellationToken cancellationToken = default)
    {
        await _dbContext.TaskTimeEntries.AddAsync(entry, cancellationToken);
    }

    public Task DeleteAsync(TaskTimeEntry entry, CancellationToken cancellationToken = default)
    {
        _dbContext.TaskTimeEntries.Remove(entry);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

