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

    public async Task<bool> ExistsBySlugAsync(Guid userId, string slug, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Companies
            .AnyAsync(c => c.UserId == userId && c.Slug == slug, cancellationToken);
    }

    public async Task ClearDefaultForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var defaultCompanies = await _dbContext.Companies
            .Where(c => c.UserId == userId && c.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var company in defaultCompanies)
        {
            company.SetDefault(false);
        }
    }

    public async Task AddAsync(Company company, CancellationToken cancellationToken = default)
    {
        await _dbContext.Companies.AddAsync(company, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

