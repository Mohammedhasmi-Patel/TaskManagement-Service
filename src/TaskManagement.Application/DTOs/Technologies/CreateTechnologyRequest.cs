using Microsoft.AspNetCore.Http;

namespace TaskManagement.Application.DTOs.Technologies;

public class CreateTechnologyRequest
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IFormFile? Logo { get; set; }
}

