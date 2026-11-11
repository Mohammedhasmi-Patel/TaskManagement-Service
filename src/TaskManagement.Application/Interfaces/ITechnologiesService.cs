using TaskManagement.Application.DTOs.Technologies;

namespace TaskManagement.Application.Interfaces;

public interface ITechnologiesService
{
    Task<TechnologyResponse> CreateAsync(CreateTechnologyRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<TechnologyResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TechnologyResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TechnologyResponse> UpdateAsync(Guid id, UpdateTechnologyRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
