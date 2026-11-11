using TaskManagement.Application.DTOs.Projects;

namespace TaskManagement.Application.Interfaces;

public interface IProjectsService
{
    Task<ProjectResponse> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProjectResponse>> GetAllAsync(Guid? companyId = null, CancellationToken cancellationToken = default);
    Task<ProjectResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProjectResponse> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

