using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Auth;
using TaskManagement.Application.DTOs.Companies;
using TaskManagement.Application.DTOs.Projects;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Application.DTOs.TimeEntries;
using TaskManagement.IntegrationTests.Common;
using Xunit;

namespace TaskManagement.IntegrationTests.Controllers;

public class TimeEntriesControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_WithoutToken_Returns401Unauthorized()
    {
        // Arrange
        var request = new CreateTimeEntryRequest
        {
            TaskId = Guid.NewGuid()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/time-entries", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_AndStop_WithEmptyStopBody_WorksAndCalculatesDuration()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        var company = await CreateCompanyAsync(token, "Acme", $"acme-{Guid.NewGuid():N}");
        var project = await CreateProjectAsync(token, company.Id, "Portal", $"portal-{Guid.NewGuid():N}");
        var task = await CreateTaskAsync(token, project.Id, "Track Work");

        var start = DateTimeOffset.UtcNow.AddMinutes(-10);
        var createRes = await SendJsonAsync(HttpMethod.Post, "/api/time-entries", token, new CreateTimeEntryRequest
        {
            TaskId = task.Id,
            StartTime = start,
            Description = "Timer started"
        });

        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var created = (await createRes.Content.ReadFromJsonAsync<ApiResponse<TimeEntryResponse>>())!.Data!;
        Assert.True(created.IsRunning);
        Assert.Equal(0, created.DurationSeconds);

        // Act - stop with no request body
        var stopReq = new HttpRequestMessage(HttpMethod.Post, $"/api/time-entries/{created.Id}/stop");
        stopReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var stopRes = await _client.SendAsync(stopReq);

        // Assert
        Assert.Equal(HttpStatusCode.OK, stopRes.StatusCode);
        var stoppedEnvelope = await stopRes.Content.ReadFromJsonAsync<ApiResponse<TimeEntryResponse>>();
        Assert.NotNull(stoppedEnvelope?.Data);
        Assert.False(stoppedEnvelope.Data.IsRunning);
        Assert.NotNull(stoppedEnvelope.Data.EndTime);
        Assert.True(stoppedEnvelope.Data.DurationSeconds >= 599);
    }

    [Fact]
    public async Task GetAll_WithTaskFilter_ReturnsOnlyFilteredEntriesForCurrentUser()
    {
        // Arrange
        var user1Token = await RegisterAndGetTokenAsync();
        var user2Token = await RegisterAndGetTokenAsync();
        var company1 = await CreateCompanyAsync(user1Token, "Co1", $"c1-{Guid.NewGuid():N}");
        var project1 = await CreateProjectAsync(user1Token, company1.Id, "P1", $"p1-{Guid.NewGuid():N}");
        var task1 = await CreateTaskAsync(user1Token, project1.Id, "Task 1");
        var task2 = await CreateTaskAsync(user1Token, project1.Id, "Task 2");

        var otherCompany = await CreateCompanyAsync(user2Token, "Other", $"o-{Guid.NewGuid():N}");
        var otherProject = await CreateProjectAsync(user2Token, otherCompany.Id, "OP", $"op-{Guid.NewGuid():N}");
        var otherTask = await CreateTaskAsync(user2Token, otherProject.Id, "Other Task");

        var baseTime = DateTimeOffset.UtcNow.AddHours(-2);
        await SendJsonAsync(HttpMethod.Post, "/api/time-entries", user1Token, new CreateTimeEntryRequest { TaskId = task1.Id, StartTime = baseTime });
        await SendJsonAsync(HttpMethod.Post, "/api/time-entries", user1Token, new CreateTimeEntryRequest { TaskId = task2.Id, StartTime = baseTime.AddMinutes(10) });
        await SendJsonAsync(HttpMethod.Post, "/api/time-entries", user2Token, new CreateTimeEntryRequest { TaskId = otherTask.Id, StartTime = baseTime.AddMinutes(20) });

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/time-entries?taskId={task1.Id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user1Token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<List<TimeEntryResponse>>>();
        Assert.NotNull(envelope?.Data);
        Assert.Single(envelope.Data);
        Assert.Equal(task1.Id, envelope.Data[0].TaskId);
    }

    [Fact]
    public async Task Update_WithValidData_Returns200OKAndRecalculatesDuration()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        var company = await CreateCompanyAsync(token, "Acme", $"acme-{Guid.NewGuid():N}");
        var project = await CreateProjectAsync(token, company.Id, "Portal", $"portal-{Guid.NewGuid():N}");
        var task = await CreateTaskAsync(token, project.Id, "Work");

        var start = new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);
        var createRes = await SendJsonAsync(HttpMethod.Post, "/api/time-entries", token, new CreateTimeEntryRequest
        {
            TaskId = task.Id,
            StartTime = start
        });
        var created = (await createRes.Content.ReadFromJsonAsync<ApiResponse<TimeEntryResponse>>())!.Data!;

        // Act
        var updateRes = await SendJsonAsync(HttpMethod.Put, $"/api/time-entries/{created.Id}", token, new UpdateTimeEntryRequest
        {
            TaskId = task.Id,
            StartTime = start,
            EndTime = start.AddMinutes(30),
            Description = "Updated entry"
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateRes.StatusCode);
        var envelope = await updateRes.Content.ReadFromJsonAsync<ApiResponse<TimeEntryResponse>>();
        Assert.NotNull(envelope?.Data);
        Assert.False(envelope.Data.IsRunning);
        Assert.Equal(1800, envelope.Data.DurationSeconds);
        Assert.Equal("Updated entry", envelope.Data.Description);
    }

    [Fact]
    public async Task Update_WhenTaskIdMismatch_Returns400BadRequest()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        var company = await CreateCompanyAsync(token, "Acme", $"acme-{Guid.NewGuid():N}");
        var project = await CreateProjectAsync(token, company.Id, "Portal", $"portal-{Guid.NewGuid():N}");
        var task = await CreateTaskAsync(token, project.Id, "Work");

        var start = new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);
        var createRes = await SendJsonAsync(HttpMethod.Post, "/api/time-entries", token, new CreateTimeEntryRequest
        {
            TaskId = task.Id,
            StartTime = start
        });
        var created = (await createRes.Content.ReadFromJsonAsync<ApiResponse<TimeEntryResponse>>())!.Data!;

        // Act
        var updateRes = await SendJsonAsync(HttpMethod.Put, $"/api/time-entries/{created.Id}", token, new UpdateTimeEntryRequest
        {
            TaskId = Guid.NewGuid(),
            StartTime = start,
            EndTime = start.AddMinutes(30)
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, updateRes.StatusCode);
        var envelope = await updateRes.Content.ReadFromJsonAsync<ApiResponse<object?>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.Success);
        Assert.Equal(400, envelope.StatusCode);
        Assert.Equal("Task ID cannot be changed for an existing time entry.", envelope.Message);
    }

    [Fact]
    public async Task Delete_WhenEntryExistsForUser_Returns200OKAndRemovesEntry()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        var company = await CreateCompanyAsync(token, "Acme", $"acme-{Guid.NewGuid():N}");
        var project = await CreateProjectAsync(token, company.Id, "Portal", $"portal-{Guid.NewGuid():N}");
        var task = await CreateTaskAsync(token, project.Id, "Work");

        var createRes = await SendJsonAsync(HttpMethod.Post, "/api/time-entries", token, new CreateTimeEntryRequest
        {
            TaskId = task.Id,
            StartTime = DateTimeOffset.UtcNow
        });
        var created = (await createRes.Content.ReadFromJsonAsync<ApiResponse<TimeEntryResponse>>())!.Data!;

        var deleteReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/time-entries/{created.Id}");
        deleteReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var deleteRes = await _client.SendAsync(deleteReq);

        // Assert
        Assert.Equal(HttpStatusCode.OK, deleteRes.StatusCode);
        var getReq = new HttpRequestMessage(HttpMethod.Get, $"/api/time-entries/{created.Id}");
        getReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var getRes = await _client.SendAsync(getReq);
        Assert.Equal(HttpStatusCode.NotFound, getRes.StatusCode);
    }

    private async Task<string> RegisterAndGetTokenAsync()
    {
        var email = $"time_user_{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest("Time", "Owner", email, "Password@123");
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        regResponse.EnsureSuccessStatusCode();

        var auth = await regResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        Assert.NotNull(auth?.Data);
        return auth.Data.AccessToken;
    }

    private async Task<CompanyResponse> CreateCompanyAsync(string token, string name, string slug)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(name), "Name");
        content.Add(new StringContent(slug), "Slug");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/companies") { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ApiResponse<CompanyResponse>>())!.Data!;
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

    private async Task<HttpResponseMessage> SendJsonAsync<T>(HttpMethod method, string url, string token, T payload)
    {
        var request = new HttpRequestMessage(method, url)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await _client.SendAsync(request);
    }
}

