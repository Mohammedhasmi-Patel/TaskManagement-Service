using System;
using System.Collections.Generic;
using System.Text;

namespace TaskManagement.Domain.Entities;

public class ProjectTechnology
{
    public Guid ProjectId { get; private set; }
    public Guid TechnologyId { get; private set; }

    public Project Project { get; private set; } = null!;
    public Technology Technology { get; private set; } = null!;
}
