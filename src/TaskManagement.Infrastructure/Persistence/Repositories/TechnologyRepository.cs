using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Repositories;

public class TechnologyRepository(ApplicationDbContext dbContext) : ITechnologyRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<Technology?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Technologies
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Technology?> GetByIdAndUserIdAsync(Guid id, Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges
            ? _dbContext.Technologies
            : _dbContext.Technologies.AsNoTracking();

        return await query
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<Technology>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Technologies
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsBySlugAsync(Guid userId, string slug, Guid? excludeTechnologyId = null, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Technologies
            .AnyAsync(
                t => t.UserId == userId
                     && t.Slug == slug
                     && (!excludeTechnologyId.HasValue || t.Id != excludeTechnologyId.Value),
                cancellationToken);
    }

    public async Task AddAsync(Technology technology, CancellationToken cancellationToken = default)
    {
        await _dbContext.Technologies.AddAsync(technology, cancellationToken);
    }

    public Task DeleteAsync(Technology technology, CancellationToken cancellationToken = default)
    {
        _dbContext.Technologies.Remove(technology);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
