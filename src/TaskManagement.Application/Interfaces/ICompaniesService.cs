using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Application.DTOs.Companies;

namespace TaskManagement.Application.Interfaces;

public interface ICompaniesService
{
    Task<CompanyResponse> CreateAsync(CreateCompanyRequest request);
    //Task<IEnumerable<CompanyResponse>> GetAllAsync();
    //Task<CompanyResponse?> GetByIdAsync(Guid id);
    //Task UpdateAsync(Guid id, UpdateCompanyRequest request);
    //Task DeleteAsync(Guid id);
}
