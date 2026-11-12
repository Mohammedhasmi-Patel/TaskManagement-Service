using TaskManagement.Application.DTOs.Dashboard;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Application.Services;

public class DashboardService(
    IDashboardRepository dashboardRepository,
    ICurrentUserService currentUserService) : IDashboardService
{
    private readonly IDashboardRepository _dashboardRepository = dashboardRepository;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<DashboardStatisticsResponse> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId
                     ?? throw new UnauthorizedAccessException("User is not authenticated.");

        return await _dashboardRepository.GetStatisticsByUserIdAsync(userId, cancellationToken);
    }
}

