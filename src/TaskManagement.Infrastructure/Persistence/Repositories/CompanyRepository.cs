using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Repositories;

public class CompanyRepository(ApplicationDbContext dbContext) : ICompanyRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Companies
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Company?> GetByIdAndUserIdAsync(Guid id, Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges
            ? _dbContext.Companies
            : _dbContext.Companies.AsNoTracking();

        return await query
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<Company>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Companies
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsBySlugAsync(Guid userId, string slug, Guid? excludeCompanyId = null, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Companies
            .AnyAsync(
                c => c.UserId == userId
                     && c.Slug == slug
                     && (!excludeCompanyId.HasValue || c.Id != excludeCompanyId.Value),
                cancellationToken);
    }

    public async Task ClearDefaultForUserAsync(Guid userId, Guid? excludeCompanyId = null, CancellationToken cancellationToken = default)
    {
        var defaultCompany = await _dbContext.Companies
            .Where(c => c.UserId == userId
                        && c.IsDefault
                        && (!excludeCompanyId.HasValue || c.Id != excludeCompanyId.Value))
            .FirstOrDefaultAsync(cancellationToken);

        if (defaultCompany is not null)
        {
            defaultCompany.SetDefault(false);
        }
    }

    public async Task AddAsync(Company company, CancellationToken cancellationToken = default)
    {
        await _dbContext.Companies.AddAsync(company, cancellationToken);
    }

    public Task DeleteAsync(Company company, CancellationToken cancellationToken = default)
    {
        _dbContext.Companies.Remove(company);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

