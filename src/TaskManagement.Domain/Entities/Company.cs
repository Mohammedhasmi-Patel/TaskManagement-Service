using System;
using System.Collections.Generic;
using System.Text;

namespace TaskManagement.Domain.Entities;

public class Company
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Logo { get; private set; }
    public string? Website { get; private set; }
    public bool IsDefault { get; private set; } = false;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; private set; }

    // Navigation Properties
    public User User { get; private set; } = null!;
    public ICollection<Project> Projects { get; private set; } = new List<Project>();
}
