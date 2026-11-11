using System;
using System.Collections.Generic;
using System.Text;

namespace TaskManagement.Domain.Entities;

public class Technology
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Logo { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    // Navigation Properties
    public ICollection<ProjectTechnology> ProjectTechnologies { get; private set; } = new List<ProjectTechnology>();
    public ICollection<TaskTechnology> TaskTechnologies { get; private set; } = new List<TaskTechnology>();

}
