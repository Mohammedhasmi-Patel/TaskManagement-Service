namespace TaskManagement.Application.DTOs.Technologies;

public record TechnologyResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? Logo
);

