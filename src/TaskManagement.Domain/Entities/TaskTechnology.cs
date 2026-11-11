using System;
using System.Collections.Generic;
using System.Text;

namespace TaskManagement.Domain.Entities;

public class TaskTechnology
{
    public Guid TaskId { get; private set; }
    public Guid TechnologyId { get; private set; }

    // Navigation Properties
    public Task Task { get; private set; } = null!;
    public Technology Technology { get; private set; } = null!;

}
