using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Entities;

public class Company : BaseEntity, IAuditableEntity
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Logo { get; private set; }
    public string? Website { get; private set; }
    public bool IsDefault { get; private set; } = false;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public User User { get; private set; } = null!;
    public ICollection<Project> Projects { get; private set; } = [];

    private Company()
    {
    }

    public static Company Create(
        Guid userId,
        string name,
        string slug,
        string? description = null,
        string? logo = null,
        string? website = null,
        bool isDefault = false)
    {
        return new Company
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            Description = description?.Trim(),
            Logo = logo?.Trim(),
            Website = website?.Trim(),
            IsDefault = isDefault,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateDetails(
        string name,
        string slug,
        string? description = null,
        string? logo = null,
        string? website = null)
    {
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Description = description?.Trim();
        Logo = logo?.Trim();
        Website = website?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetDefault(bool isDefault)
    {
        IsDefault = isDefault;
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
