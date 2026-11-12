using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.DTOs.Dashboard;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Infrastructure.Persistence.Repositories;

public class DashboardRepository(ApplicationDbContext dbContext) : IDashboardRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<DashboardStatisticsResponse> GetStatisticsByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var totalCompanies = await _dbContext.Companies
            .AsNoTracking()
            .CountAsync(c => c.UserId == userId, cancellationToken);

        var totalTechnologies = await _dbContext.Technologies
            .AsNoTracking()
            .CountAsync(t => t.UserId == userId, cancellationToken);

        var totalProjects = await _dbContext.Projects
            .AsNoTracking()
            .CountAsync(p => p.Company.UserId == userId, cancellationToken);

        var totalTasks = await _dbContext.Tasks
            .AsNoTracking()
            .CountAsync(t => t.Project.Company.UserId == userId, cancellationToken);

        var totalTimeEntries = await _dbContext.TaskTimeEntries
            .AsNoTracking()
            .CountAsync(e => e.Task.Project.Company.UserId == userId, cancellationToken);

        return new DashboardStatisticsResponse(
            TotalCompanies: totalCompanies,
            TotalTechnologies: totalTechnologies,
            TotalProjects: totalProjects,
            TotalTasks: totalTasks,
            TotalTimeEntries: totalTimeEntries);
    }
}
