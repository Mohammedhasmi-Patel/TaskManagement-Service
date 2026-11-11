using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Repositories;

public class ProjectRepository(ApplicationDbContext dbContext) : IProjectRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Projects
            .Include(p => p.ProjectTechnologies)
                .ThenInclude(pt => pt.Technology)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Project?> GetByIdAndUserIdAsync(Guid id, Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges
            ? _dbContext.Projects
            : _dbContext.Projects.AsNoTracking();

        return await query
            .Include(p => p.ProjectTechnologies)
                .ThenInclude(pt => pt.Technology)
            .FirstOrDefaultAsync(p => p.Id == id && p.Company.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> GetAllByUserIdAsync(Guid userId, Guid? companyId = null, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Projects
            .AsNoTracking()
            .Include(p => p.ProjectTechnologies)
                .ThenInclude(pt => pt.Technology)
            .Where(p => p.Company.UserId == userId && (!companyId.HasValue || p.CompanyId == companyId.Value))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsBySlugAsync(Guid companyId, string slug, Guid? excludeProjectId = null, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Projects
            .AnyAsync(
                p => p.CompanyId == companyId
                     && p.Slug == slug
                     && (!excludeProjectId.HasValue || p.Id != excludeProjectId.Value),
                cancellationToken);
    }

    public async Task AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        await _dbContext.Projects.AddAsync(project, cancellationToken);
    }

    public Task DeleteAsync(Project project, CancellationToken cancellationToken = default)
    {
        _dbContext.Projects.Remove(project);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

