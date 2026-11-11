using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Auth;
using TaskManagement.Application.DTOs.Companies;
using TaskManagement.Application.DTOs.Projects;
using TaskManagement.Application.DTOs.Technologies;
using TaskManagement.Domain.Enums;
using TaskManagement.IntegrationTests.Common;
using Xunit;

namespace TaskManagement.IntegrationTests.Controllers;

public class ProjectsControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_WithoutToken_Returns401Unauthorized()
    {
        // Arrange
        var request = new CreateProjectRequest
        {
            CompanyId = Guid.NewGuid(),
            Name = "Project One",
            Slug = "project-one"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/projects", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithValidDataAndTechnologies_Returns201CreatedAndProject()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        var company = await CreateCompanyAsync(token, "Acme", $"acme-{Guid.NewGuid():N}");
        var tech = await CreateTechnologyAsync(token, ".NET", $"dotnet-{Guid.NewGuid():N}");
        var slug = $"proj-{Guid.NewGuid():N}";

        var body = new CreateProjectRequest
        {
            CompanyId = company.Id,
            Name = "Portal API",
            Slug = slug,
            Description = "Backend platform",
            Status = ProjectStatus.InProgress,
            StartDate = new DateOnly(2026, 3, 1),
            EndDate = new DateOnly(2026, 5, 1),
            TechnologyIds = [tech.Id]
        };

        // Act
        var response = await SendJsonAsync(HttpMethod.Post, "/api/projects", token, body);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<ProjectResponse>>();
        Assert.NotNull(envelope?.Data);
        Assert.True(envelope.Success);
        Assert.Equal(201, envelope.StatusCode);
        Assert.Equal(company.Id, envelope.Data.CompanyId);
        Assert.Equal("Portal API", envelope.Data.Name);
        Assert.Equal(slug, envelope.Data.Slug);
        Assert.Equal("InProgress", envelope.Data.Status);
        Assert.Single(envelope.Data.Technologies);
        Assert.Equal(tech.Id, envelope.Data.Technologies[0].Id);
    }

    [Fact]
    public async Task Create_WithDuplicateSlugInSameCompany_Returns409Conflict()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        var company = await CreateCompanyAsync(token, "Acme", $"acme-{Guid.NewGuid():N}");
        var slug = $"dup-{Guid.NewGuid():N}";

        var first = await SendJsonAsync(HttpMethod.Post, "/api/projects", token, new CreateProjectRequest
        {
            CompanyId = company.Id,
            Name = "First",
            Slug = slug
        });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        // Act
        var second = await SendJsonAsync(HttpMethod.Post, "/api/projects", token, new CreateProjectRequest
        {
            CompanyId = company.Id,
            Name = "Second",
            Slug = slug
        });

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidDateRange_Returns400BadRequestWithFirstErrorMessage()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        var company = await CreateCompanyAsync(token, "Acme", $"acme-{Guid.NewGuid():N}");

        // Act
        var response = await SendJsonAsync(HttpMethod.Post, "/api/projects", token, new CreateProjectRequest
        {
            CompanyId = company.Id,
            Name = "Portal",
            Slug = $"proj-{Guid.NewGuid():N}",
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 5, 1)
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<object?>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.Success);
        Assert.Equal(400, envelope.StatusCode);
        Assert.Equal("End date must be on or after start date.", envelope.Message);
    }

    [Fact]
    public async Task GetAll_WithCompanyIdFilter_ReturnsOnlyFilteredProjectsForCurrentUser()
    {
        // Arrange
        var user1Token = await RegisterAndGetTokenAsync();
        var user2Token = await RegisterAndGetTokenAsync();
        var company1 = await CreateCompanyAsync(user1Token, "Co 1", $"c1-{Guid.NewGuid():N}");
        var company2 = await CreateCompanyAsync(user1Token, "Co 2", $"c2-{Guid.NewGuid():N}");
        var otherCompany = await CreateCompanyAsync(user2Token, "Other", $"o-{Guid.NewGuid():N}");

        await SendJsonAsync(HttpMethod.Post, "/api/projects", user1Token, new CreateProjectRequest { CompanyId = company1.Id, Name = "Zeta", Slug = $"z-{Guid.NewGuid():N}" });
        await SendJsonAsync(HttpMethod.Post, "/api/projects", user1Token, new CreateProjectRequest { CompanyId = company1.Id, Name = "Alpha", Slug = $"a-{Guid.NewGuid():N}" });
        await SendJsonAsync(HttpMethod.Post, "/api/projects", user1Token, new CreateProjectRequest { CompanyId = company2.Id, Name = "Beta", Slug = $"b-{Guid.NewGuid():N}" });
        await SendJsonAsync(HttpMethod.Post, "/api/projects", user2Token, new CreateProjectRequest { CompanyId = otherCompany.Id, Name = "Hidden", Slug = $"h-{Guid.NewGuid():N}" });

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/projects?companyId={company1.Id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user1Token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<List<ProjectResponse>>>();
        Assert.NotNull(envelope?.Data);
        Assert.Equal(2, envelope.Data.Count);
        Assert.Equal(["Alpha", "Zeta"], envelope.Data.Select(p => p.Name));
    }

    [Fact]
    public async Task GetById_WhenProjectBelongsToAnotherUser_Returns404NotFound()
    {
        // Arrange
        var ownerToken = await RegisterAndGetTokenAsync();
        var otherToken = await RegisterAndGetTokenAsync();
        var company = await CreateCompanyAsync(ownerToken, "Acme", $"acme-{Guid.NewGuid():N}");
        var createRes = await SendJsonAsync(HttpMethod.Post, "/api/projects", ownerToken, new CreateProjectRequest
        {
            CompanyId = company.Id,
            Name = "Private",
            Slug = $"priv-{Guid.NewGuid():N}"
        });
        var created = (await createRes.Content.ReadFromJsonAsync<ApiResponse<ProjectResponse>>())!.Data!;

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/projects/{created.Id}");
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
        var tech1 = await CreateTechnologyAsync(token, "C#", $"cs-{Guid.NewGuid():N}");
        var tech2 = await CreateTechnologyAsync(token, "Redis", $"rd-{Guid.NewGuid():N}");

        var createRes = await SendJsonAsync(HttpMethod.Post, "/api/projects", token, new CreateProjectRequest
        {
            CompanyId = company.Id,
            Name = "Old Name",
            Slug = $"old-{Guid.NewGuid():N}",
            TechnologyIds = [tech1.Id]
        });
        var created = (await createRes.Content.ReadFromJsonAsync<ApiResponse<ProjectResponse>>())!.Data!;

        var updatedSlug = $"new-{Guid.NewGuid():N}";
        var updateReq = new UpdateProjectRequest
        {
            CompanyId = company.Id,
            Name = "New Name",
            Slug = updatedSlug,
            Status = ProjectStatus.Completed,
            IsActive = false,
            TechnologyIds = [tech2.Id]
        };

        // Act
        var response = await SendJsonAsync(HttpMethod.Put, $"/api/projects/{created.Id}", token, updateReq);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<ProjectResponse>>();
        Assert.NotNull(envelope?.Data);
        Assert.Equal("New Name", envelope.Data.Name);
        Assert.Equal(updatedSlug, envelope.Data.Slug);
        Assert.Equal("Completed", envelope.Data.Status);
        Assert.False(envelope.Data.IsActive);
        Assert.Single(envelope.Data.Technologies);
        Assert.Equal(tech2.Id, envelope.Data.Technologies[0].Id);
    }

    [Fact]
    public async Task Delete_WhenProjectExistsForUser_Returns200OKAndRemovesProject()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        var company = await CreateCompanyAsync(token, "Acme", $"acme-{Guid.NewGuid():N}");
        var createRes = await SendJsonAsync(HttpMethod.Post, "/api/projects", token, new CreateProjectRequest
        {
            CompanyId = company.Id,
            Name = "Delete Me",
            Slug = $"del-{Guid.NewGuid():N}"
        });
        var created = (await createRes.Content.ReadFromJsonAsync<ApiResponse<ProjectResponse>>())!.Data!;

        var deleteReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/projects/{created.Id}");
        deleteReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var deleteRes = await _client.SendAsync(deleteReq);

        // Assert
        Assert.Equal(HttpStatusCode.OK, deleteRes.StatusCode);
        var getReq = new HttpRequestMessage(HttpMethod.Get, $"/api/projects/{created.Id}");
        getReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var getRes = await _client.SendAsync(getReq);
        Assert.Equal(HttpStatusCode.NotFound, getRes.StatusCode);
    }

    private async Task<string> RegisterAndGetTokenAsync()
    {
        var email = $"project_user_{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest("Project", "Owner", email, "Password@123");
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

