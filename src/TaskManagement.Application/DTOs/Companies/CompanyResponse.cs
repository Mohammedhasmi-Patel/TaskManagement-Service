using System;
using System.Collections.Generic;
using System.Text;

namespace TaskManagement.Application.DTOs.Companies;

public record CompanyResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Logo,
    string? Website,
    string? Description,
    bool IsDefault
);
