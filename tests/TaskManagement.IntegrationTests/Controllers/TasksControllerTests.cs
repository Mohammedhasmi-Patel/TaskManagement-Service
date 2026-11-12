using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Auth;
using TaskManagement.Application.DTOs.Companies;
using TaskManagement.Application.DTOs.Projects;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Application.DTOs.Technologies;
using TaskManagement.Domain.Enums;
using TaskManagement.IntegrationTests.Common;
using Xunit;

namespace TaskManagement.IntegrationTests.Controllers;

public class TasksControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_WithoutToken_Returns401Unauthorized()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            ProjectId = Guid.NewGuid(),
            Title = "Task One"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/tasks", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithValidDataAndTechnologies_Returns201CreatedAndTask()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        var company = await CreateCompanyAsync(token, "Acme", $"acme-{Guid.NewGuid():N}");
        var project = await CreateProjectAsync(token, company.Id, "Portal", $"portal-{Guid.NewGuid():N}");
        var tech = await CreateTechnologyAsync(token, ".NET", $"dotnet-{Guid.NewGuid():N}");

        var body = new CreateTaskRequest
        {
            ProjectId = project.Id,
            Title = "Setup Swagger",
            Description = "Configure OpenAPI docs",
            Status = TaskStatusEnum.InProgress,
            Priority = TaskPriority.High,
            EstimatedHours = 3.5m,
            TechnologyIds = [tech.Id]
        };

        // Act
        var response = await SendJsonAsync(HttpMethod.Post, "/api/tasks", token, body);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<TaskResponse>>();
        Assert.NotNull(envelope?.Data);
        Assert.True(envelope.Success);
        Assert.Equal(201, envelope.StatusCode);
        Assert.Equal(project.Id, envelope.Data.ProjectId);
        Assert.Equal("Setup Swagger", envelope.Data.Title);
        Assert.Equal("InProgress", envelope.Data.Status);
        Assert.Equal("High", envelope.Data.Priority);
        Assert.Equal(3.5m, envelope.Data.EstimatedHours);
        Assert.Single(envelope.Data.Technologies);
        Assert.Equal(tech.Id, envelope.Data.Technologies[0].Id);
    }

    [Fact]
    public async Task Create_WithInvalidData_Returns400BadRequestWithFirstErrorMessage()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        var company = await CreateCompanyAsync(token, "Acme", $"acme-{Guid.NewGuid():N}");
        var project = await CreateProjectAsync(token, company.Id, "Portal", $"portal-{Guid.NewGuid():N}");

        // Act
        var response = await SendJsonAsync(HttpMethod.Post, "/api/tasks", token, new CreateTaskRequest
        {
            ProjectId = project.Id,
            Title = "Valid Title",
            EstimatedHours = -2m
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<object?>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.Success);
        Assert.Equal(400, envelope.StatusCode);
        Assert.Equal("Estimated hours cannot be negative.", envelope.Message);
    }

    [Fact]
    public async Task GetAll_WithFilters_ReturnsOnlyFilteredTasksForCurrentUser()
    {
        // Arrange
        var user1Token = await RegisterAndGetTokenAsync();
        var user2Token = await RegisterAndGetTokenAsync();
        var company1 = await CreateCompanyAsync(user1Token, "Co 1", $"c1-{Guid.NewGuid():N}");
        var project1 = await CreateProjectAsync(user1Token, company1.Id, "P1", $"p1-{Guid.NewGuid():N}");
        var otherCompany = await CreateCompanyAsync(user2Token, "Other", $"o-{Guid.NewGuid():N}");
        var otherProject = await CreateProjectAsync(user2Token, otherCompany.Id, "OP", $"op-{Guid.NewGuid():N}");

        await SendJsonAsync(HttpMethod.Post, "/api/tasks", user1Token, new CreateTaskRequest { ProjectId = project1.Id, Title = "Todo Task", Status = TaskStatusEnum.Todo, Priority = TaskPriority.Low });
        await SendJsonAsync(HttpMethod.Post, "/api/tasks", user1Token, new CreateTaskRequest { ProjectId = project1.Id, Title = "Active Task", Status = TaskStatusEnum.InProgress, Priority = TaskPriority.High });
        await SendJsonAsync(HttpMethod.Post, "/api/tasks", user2Token, new CreateTaskRequest { ProjectId = otherProject.Id, Title = "Hidden Task", Status = TaskStatusEnum.InProgress, Priority = TaskPriority.High });

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/tasks?projectId={project1.Id}&status=InProgress&priority=High");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user1Token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<List<TaskResponse>>>();
        Assert.NotNull(envelope?.Data);
        Assert.Single(envelope.Data);
        Assert.Equal("Active Task", envelope.Data[0].Title);
    }

    [Fact]
    public async Task GetById_WhenTaskBelongsToAnotherUser_Returns404NotFound()
    {
        // Arrange
        var ownerToken = await RegisterAndGetTokenAsync();
        var otherToken = await RegisterAndGetTokenAsync();
        var company = await CreateCompanyAsync(ownerToken, "Acme", $"acme-{Guid.NewGuid():N}");
        var project = await CreateProjectAsync(ownerToken, company.Id, "Portal", $"portal-{Guid.NewGuid():N}");
        var createRes = await SendJsonAsync(HttpMethod.Post, "/api/tasks", ownerToken, new CreateTaskRequest
        {
            ProjectId = project.Id,
            Title = "Private Task"
        });
        var created = (await createRes.Content.ReadFromJsonAsync<ApiResponse<TaskResponse>>())!.Data!;

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/tasks/{created.Id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithValidDataAndTechnologies_Returns200OK()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        var company = await CreateCompanyAsync(token, "Acme", $"acme-{Guid.NewGuid():N}");
        var project = await CreateProjectAsync(token, company.Id, "Portal", $"portal-{Guid.NewGuid():N}");
        var tech1 = await CreateTechnologyAsync(token, "C#", $"cs-{Guid.NewGuid():N}");
        var tech2 = await CreateTechnologyAsync(token, "Redis", $"rd-{Guid.NewGuid():N}");

        var createRes = await SendJsonAsync(HttpMethod.Post, "/api/tasks", token, new CreateTaskRequest
        {
            ProjectId = project.Id,
            Title = "Old Task",
            TechnologyIds = [tech1.Id]
        });
        var created = (await createRes.Content.ReadFromJsonAsync<ApiResponse<TaskResponse>>())!.Data!;

        var updateReq = new UpdateTaskRequest
        {
            ProjectId = project.Id,
            Title = "Updated Task",
            Description = "Completed feature",
            Status = TaskStatusEnum.Completed,
            Priority = TaskPriority.Critical,
            EstimatedHours = 5m,
            TechnologyIds = [tech2.Id]
        };

        // Act
        var response = await SendJsonAsync(HttpMethod.Put, $"/api/tasks/{created.Id}", token, updateReq);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<TaskResponse>>();
        Assert.NotNull(envelope?.Data);
        Assert.Equal("Updated Task", envelope.Data.Title);
        Assert.Equal("Completed", envelope.Data.Status);
        Assert.Equal("Critical", envelope.Data.Priority);
        Assert.Equal(5m, envelope.Data.EstimatedHours);
        Assert.Single(envelope.Data.Technologies);
        Assert.Equal(tech2.Id, envelope.Data.Technologies[0].Id);
    }

    [Fact]
    public async Task Delete_WhenTaskExistsForUser_Returns200OKAndRemovesTask()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        var company = await CreateCompanyAsync(token, "Acme", $"acme-{Guid.NewGuid():N}");
        var project = await CreateProjectAsync(token, company.Id, "Portal", $"portal-{Guid.NewGuid():N}");
        var createRes = await SendJsonAsync(HttpMethod.Post, "/api/tasks", token, new CreateTaskRequest
        {
            ProjectId = project.Id,
            Title = "Delete Task"
        });
        var created = (await createRes.Content.ReadFromJsonAsync<ApiResponse<TaskResponse>>())!.Data!;

        var deleteReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/tasks/{created.Id}");
        deleteReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var deleteRes = await _client.SendAsync(deleteReq);

        // Assert
        Assert.Equal(HttpStatusCode.OK, deleteRes.StatusCode);
        var getReq = new HttpRequestMessage(HttpMethod.Get, $"/api/tasks/{created.Id}");
        getReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var getRes = await _client.SendAsync(getReq);
        Assert.Equal(HttpStatusCode.NotFound, getRes.StatusCode);
    }

    private async Task<string> RegisterAndGetTokenAsync()
    {
        var email = $"task_user_{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest("Task", "Owner", email, "Password@123");
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

    private async Task<TechnologyResponse> CreateTechnologyAsync(string token, string name, string slug)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(name), "Name");
        content.Add(new StringContent(slug), "Slug");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/technologies") { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ApiResponse<TechnologyResponse>>())!.Data!;
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

