using TaskManagement.Application.DTOs.Dashboard;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using Xunit;

namespace TaskManagement.UnitTests.Dashboard;

public class DashboardServiceTests
{
    private readonly FakeDashboardRepository _dashboardRepository = new();
    private readonly FakeCurrentUserService _currentUserService = new();
    private readonly DashboardService _service;

    public DashboardServiceTests()
    {
        _service = new DashboardService(_dashboardRepository, _currentUserService);
    }

    [Fact]
    public async Task GetStatisticsAsync_WhenAuthenticated_ShouldReturnUserStatistics()
    {
        // Arrange
        var expected = new DashboardStatisticsResponse(
            TotalCompanies: 2,
            TotalTechnologies: 4,
            TotalProjects: 3,
            TotalTasks: 5,
            TotalTimeEntries: 6);

        _dashboardRepository.ResponseToReturn = expected;

        // Act
        var result = await _service.GetStatisticsAsync();

        // Assert
        Assert.Equal(_currentUserService.UserId, _dashboardRepository.LastRequestedUserId);
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetStatisticsAsync_WhenUnauthenticated_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        _currentUserService.UserId = null;

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.GetStatisticsAsync());
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? UserId { get; set; } = Guid.NewGuid();
        public string? Email => "user@example.com";
        public string? Role => "User";
        public bool IsAuthenticated => UserId.HasValue;
    }

    private sealed class FakeDashboardRepository : IDashboardRepository
    {
        public Guid? LastRequestedUserId { get; private set; }
        public DashboardStatisticsResponse ResponseToReturn { get; set; } = new(0, 0, 0, 0, 0);

        public Task<DashboardStatisticsResponse> GetStatisticsByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            LastRequestedUserId = userId;
            return Task.FromResult(ResponseToReturn);
        }
    }
}
