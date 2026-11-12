namespace TaskManagement.Application.DTOs.Dashboard;

public record DashboardStatisticsResponse(
    int TotalCompanies,
    int TotalTechnologies,
    int TotalProjects,
    int TotalTasks,
    int TotalTimeEntries
);
