using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Interfaces;

public interface ITechnologyRepository
{
    Task<Technology?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Technology?> GetByIdAndUserIdAsync(Guid id, Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Technology>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsBySlugAsync(Guid userId, string slug, Guid? excludeTechnologyId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Technology technology, CancellationToken cancellationToken = default);
    Task DeleteAsync(Technology technology, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
