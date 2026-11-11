using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Interfaces;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Project?> GetByIdAndUserIdAsync(Guid id, Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> GetAllByUserIdAsync(Guid userId, Guid? companyId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsBySlugAsync(Guid companyId, string slug, Guid? excludeProjectId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Project project, CancellationToken cancellationToken = default);
    Task DeleteAsync(Project project, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

