using TaskManagement.Domain.Common;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Entities;

public class Project : BaseEntity, IAuditableEntity
{
    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ProjectStatus Status { get; private set; } = ProjectStatus.Planned;

    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public bool IsActive { get; private set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public Company Company { get; private set; } = null!;
    public ICollection<ProjectTechnology> ProjectTechnologies { get; private set; } = [];
    public ICollection<TaskItem> Tasks { get; private set; } = [];

    private Project()
    {
    }

    public static Project Create(
        Guid companyId,
        string name,
        string slug,
        string? description = null,
        ProjectStatus status = ProjectStatus.Planned,
        DateOnly? startDate = null,
        DateOnly? endDate = null)
    {
        return new Project
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            Description = description?.Trim(),
            Status = status,
            StartDate = startDate,
            EndDate = endDate,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateDetails(
        string name,
        string slug,
        string? description = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null)
    {
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Description = description?.Trim();
        StartDate = startDate;
        EndDate = endDate;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateStatus(ProjectStatus status)
    {
        Status = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
