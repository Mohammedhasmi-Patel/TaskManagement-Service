using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Application.DTOs.Companies;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Application.Services;

public class CompaniesService : ICompaniesService
{
    public Task<CompanyResponse> CreateAsync(CreateCompanyRequest request)
    {
        throw new NotImplementedException();
    }
}
