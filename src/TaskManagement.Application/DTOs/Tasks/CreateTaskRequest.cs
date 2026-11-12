using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.DTOs.Tasks;

public class CreateTaskRequest
{
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskStatusEnum Status { get; set; } = TaskStatusEnum.Todo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public decimal? EstimatedHours { get; set; }
    public List<Guid>? TechnologyIds { get; set; }
}

