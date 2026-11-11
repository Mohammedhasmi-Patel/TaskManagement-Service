using TaskManagement.Application.DTOs.Technologies;

namespace TaskManagement.Application.DTOs.Projects;

public record ProjectResponse(
    Guid Id,
    Guid CompanyId,
    string Name,
    string Slug,
    string? Description,
    string Status,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool IsActive,
    IReadOnlyList<TechnologyResponse> Technologies
);

