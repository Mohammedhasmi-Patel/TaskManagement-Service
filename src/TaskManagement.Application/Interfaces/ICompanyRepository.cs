using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Interfaces;

public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Company?> GetByIdAndUserIdAsync(Guid id, Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Company>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsBySlugAsync(Guid userId, string slug, Guid? excludeCompanyId = null, CancellationToken cancellationToken = default);
    Task ClearDefaultForUserAsync(Guid userId, Guid? excludeCompanyId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Company company, CancellationToken cancellationToken = default);
    Task DeleteAsync(Company company, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

