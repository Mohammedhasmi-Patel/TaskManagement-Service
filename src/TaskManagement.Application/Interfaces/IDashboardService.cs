using TaskManagement.Application.DTOs.Dashboard;

namespace TaskManagement.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatisticsResponse> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

