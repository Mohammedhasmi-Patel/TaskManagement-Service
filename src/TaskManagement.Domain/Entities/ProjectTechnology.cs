using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Entities;

public class ProjectTechnology : IAuditableEntity
{
    public Guid ProjectId { get; private set; }
    public Guid TechnologyId { get; private set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public Project Project { get; private set; } = null!;
    public Technology Technology { get; private set; } = null!;

    private ProjectTechnology()
    {
    }

    public static ProjectTechnology Create(Guid projectId, Guid technologyId)
    {
        return new ProjectTechnology
        {
            ProjectId = projectId,
            TechnologyId = technologyId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
