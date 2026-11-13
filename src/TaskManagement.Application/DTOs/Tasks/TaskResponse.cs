using TaskManagement.Application.DTOs.Technologies;

namespace TaskManagement.Application.DTOs.Tasks;

public record TaskResponse(
    Guid Id,
    Guid ProjectId,
    string Title,
    string? Description,
    string Status,
    string Priority,
    decimal? EstimatedHours,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<TechnologyResponse> Technologies,
    IReadOnlyList<TaskTimeEntryResponse> TimeEntries
);
