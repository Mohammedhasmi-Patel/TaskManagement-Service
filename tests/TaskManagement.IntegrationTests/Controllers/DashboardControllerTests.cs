using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Auth;
using TaskManagement.Application.DTOs.Companies;
using TaskManagement.Application.DTOs.Dashboard;
using TaskManagement.Application.DTOs.Projects;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Application.DTOs.TimeEntries;
using TaskManagement.IntegrationTests.Common;
using Xunit;

namespace TaskManagement.IntegrationTests.Controllers;

public class DashboardControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetStatistics_WithoutToken_Returns401Unauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/dashboard/statistics");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetStatistics_WithNoData_ReturnsZeroedCountsForUser()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/dashboard/statistics");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<DashboardStatisticsResponse>>();
        Assert.NotNull(envelope?.Data);
        Assert.True(envelope.Success);
        Assert.Equal(new DashboardStatisticsResponse(0, 0, 0, 0, 0), envelope.Data);
    }

    [Fact]
    public async Task GetStatistics_ReturnsCountsScopedToAuthenticatedUser()
    {
        // Arrange
        var user1Token = await RegisterAndGetTokenAsync();
        var user2Token = await RegisterAndGetTokenAsync();

        var company1 = await CreateCompanyAsync(user1Token, "Alpha Co", $"alpha-{Guid.NewGuid():N}");
        await CreateCompanyAsync(user1Token, "Beta Co", $"beta-{Guid.NewGuid():N}");

        await CreateTechnologyAsync(user1Token, "CSharp", $"csharp-{Guid.NewGuid():N}");
        await CreateTechnologyAsync(user1Token, "PostgreSQL", $"pg-{Guid.NewGuid():N}");

        var project1 = await CreateProjectAsync(user1Token, company1.Id, "Platform", $"platform-{Guid.NewGuid():N}");
        await CreateProjectAsync(user1Token, company1.Id, "Portal", $"portal-{Guid.NewGuid():N}");

        var task1 = await CreateTaskAsync(user1Token, project1.Id, "Build API");
        var task2 = await CreateTaskAsync(user1Token, project1.Id, "Write Tests");
        await CreateTaskAsync(user1Token, project1.Id, "Deploy");

        var start = DateTimeOffset.UtcNow.AddHours(-1);
        await CreateTimeEntryAsync(user1Token, task1.Id, start);
        await CreateTimeEntryAsync(user1Token, task2.Id, start.AddMinutes(10));

        // Seed another user's data that must be excluded
        var otherCompany = await CreateCompanyAsync(user2Token, "Other Co", $"other-{Guid.NewGuid():N}");
        await CreateTechnologyAsync(user2Token, "Go", $"go-{Guid.NewGuid():N}");
        var otherProject = await CreateProjectAsync(user2Token, otherCompany.Id, "Other Proj", $"otherp-{Guid.NewGuid():N}");
        var otherTask = await CreateTaskAsync(user2Token, otherProject.Id, "Other Task");
        await CreateTimeEntryAsync(user2Token, otherTask.Id, start);

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/dashboard/statistics");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user1Token);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<DashboardStatisticsResponse>>();
        Assert.NotNull(envelope?.Data);

        Assert.Equal(2, envelope.Data.TotalCompanies);
        Assert.Equal(2, envelope.Data.TotalTechnologies);
        Assert.Equal(2, envelope.Data.TotalProjects);
        Assert.Equal(3, envelope.Data.TotalTasks);
        Assert.Equal(2, envelope.Data.TotalTimeEntries);
    }

    private async Task<string> RegisterAndGetTokenAsync()
    {
        var email = $"dash-{Guid.NewGuid():N}@example.com";
        var registerRes = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Dash", "Tester", email, "Password123!"));
        registerRes.EnsureSuccessStatusCode();
        var auth = await registerRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        return auth!.Data!.AccessToken;
    }

    private async Task<CompanyResponse> CreateCompanyAsync(string token, string name, string slug)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(name), "Name");
        form.Add(new StringContent(slug), "Slug");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/companies") { Content = form };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ApiResponse<CompanyResponse>>())!.Data!;
    }

    private async Task CreateTechnologyAsync(string token, string name, string slug)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(name), "Name");
        form.Add(new StringContent(slug), "Slug");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/technologies") { Content = form };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private async Task<ProjectResponse> CreateProjectAsync(string token, Guid companyId, string name, string slug)
    {
        var response = await SendJsonAsync(HttpMethod.Post, "/api/projects", token, new CreateProjectRequest
        {
            CompanyId = companyId,
            Name = name,
            Slug = slug
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ApiResponse<ProjectResponse>>())!.Data!;
    }

    private async Task<TaskResponse> CreateTaskAsync(string token, Guid projectId, string title)
    {
        var response = await SendJsonAsync(HttpMethod.Post, "/api/tasks", token, new CreateTaskRequest
        {
            ProjectId = projectId,
            Title = title
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ApiResponse<TaskResponse>>())!.Data!;
    }

    private async Task CreateTimeEntryAsync(string token, Guid taskId, DateTimeOffset startTime)
    {
        var response = await SendJsonAsync(HttpMethod.Post, "/api/time-entries", token, new CreateTimeEntryRequest
        {
            TaskId = taskId,
            StartTime = startTime
        });
        response.EnsureSuccessStatusCode();
    }

    private async Task<HttpResponseMessage> SendJsonAsync<T>(HttpMethod method, string url, string token, T body)
    {
        var request = new HttpRequestMessage(method, url)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }
}
