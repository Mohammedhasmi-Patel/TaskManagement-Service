using TaskManagement.Application.DTOs.Companies;

namespace TaskManagement.Application.Interfaces;

public interface ICompaniesService
{
    Task<CompanyResponse> CreateAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<CompanyResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CompanyResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    //Task UpdateAsync(Guid id, UpdateCompanyRequest request);
    //Task DeleteAsync(Guid id);
}
