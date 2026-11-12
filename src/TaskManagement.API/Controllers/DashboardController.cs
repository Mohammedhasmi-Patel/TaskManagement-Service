using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Dashboard;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.API.Controllers;

[Authorize]
public class DashboardController(IDashboardService dashboardService) : BaseApiController
{
    private readonly IDashboardService _dashboardService = dashboardService;

    /// <summary>
    /// Gets aggregated dashboard statistics for the authenticated user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Counts and summaries across companies, technologies, projects, tasks, and time entries.</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ApiResponse<DashboardStatisticsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<DashboardStatisticsResponse>>> GetStatistics(CancellationToken cancellationToken)
    {
        var response = await _dashboardService.GetStatisticsAsync(cancellationToken);
        return OkResponse(response, "Dashboard statistics retrieved successfully.");
    }
}

