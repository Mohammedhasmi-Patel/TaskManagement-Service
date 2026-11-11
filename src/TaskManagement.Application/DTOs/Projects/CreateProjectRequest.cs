using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.DTOs.Projects;

public class CreateProjectRequest
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Planned;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public List<Guid>? TechnologyIds { get; set; }
}

