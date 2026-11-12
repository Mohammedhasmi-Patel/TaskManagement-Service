using TaskManagement.Application.DTOs.TimeEntries;

namespace TaskManagement.Application.Interfaces;

public interface ITimeEntriesService
{
    Task<TimeEntryResponse> CreateAsync(CreateTimeEntryRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<TimeEntryResponse>> GetAllAsync(Guid? taskId = null, CancellationToken cancellationToken = default);
    Task<TimeEntryResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TimeEntryResponse> UpdateAsync(Guid id, UpdateTimeEntryRequest request, CancellationToken cancellationToken = default);
    Task<TimeEntryResponse> StopAsync(Guid id, StopTimeEntryRequest? request = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

