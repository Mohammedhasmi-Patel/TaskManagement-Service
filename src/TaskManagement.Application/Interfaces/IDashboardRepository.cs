using TaskManagement.Application.DTOs.Dashboard;

namespace TaskManagement.Application.Interfaces;

public interface IDashboardRepository
{
    Task<DashboardStatisticsResponse> GetStatisticsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}

