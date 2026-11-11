using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Entities;

public class Technology : BaseEntity, IAuditableEntity
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Logo { get; private set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation Properties
    public ICollection<ProjectTechnology> ProjectTechnologies { get; private set; } = [];
    public ICollection<TaskTechnology> TaskTechnologies { get; private set; } = [];

    // Required by EF Core
    private Technology()
    {
    }

    public static Technology Create(
        string name,
        string slug,
        string? description = null,
        string? logo = null)
    {
        return new Technology
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            Description = description?.Trim(),
            Logo = logo?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateDetails(
        string name,
        string slug,
        string? description = null,
        string? logo = null)
    {
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Description = description?.Trim();
        Logo = logo?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
