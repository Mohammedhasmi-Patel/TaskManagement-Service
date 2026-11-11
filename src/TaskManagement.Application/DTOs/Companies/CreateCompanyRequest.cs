using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace TaskManagement.Application.DTOs.Companies;

public class CreateCompanyRequest
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IFormFile? Logo { get; set; }
    public string? Website { get; set; }
    public bool IsDefault { get; set; } = false;
}
