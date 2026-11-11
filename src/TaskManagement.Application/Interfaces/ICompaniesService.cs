using TaskManagement.Application.DTOs.Companies;

namespace TaskManagement.Application.Interfaces;

public interface ICompaniesService
{
    Task<CompanyResponse> CreateAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default);
    //Task<IEnumerable<CompanyResponse>> GetAllAsync();
    //Task<CompanyResponse?> GetByIdAsync(Guid id);
    //Task UpdateAsync(Guid id, UpdateCompanyRequest request);
    //Task DeleteAsync(Guid id);
}
