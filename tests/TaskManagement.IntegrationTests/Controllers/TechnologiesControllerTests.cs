using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Auth;
using TaskManagement.Application.DTOs.Technologies;
using TaskManagement.Application.Interfaces;
using TaskManagement.IntegrationTests.Common;
using Xunit;

namespace TaskManagement.IntegrationTests.Controllers;

public class TechnologiesControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_WithoutToken_Returns401Unauthorized()
    {
        // Arrange
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("DotNet"), "Name");
        content.Add(new StringContent("dotnet"), "Slug");

        // Act
        var response = await _client.PostAsync("/api/technologies", content);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<object?>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.Success);
        Assert.Equal(401, envelope.StatusCode);
    }

    [Fact]
    public async Task Create_WithValidData_Returns201CreatedAndTechnology()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        var slug = $"tech-{Guid.NewGuid():N}";
        string? uploadedLogoUrl = null;

        try
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent("PostgreSQL"), "Name");
            content.Add(new StringContent(slug), "Slug");
            content.Add(new StringContent("Relational database"), "Description");

            var fileContent = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47]);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(fileContent, "Logo", "pg.png");

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/technologies")
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<TechnologyResponse>>();
            Assert.NotNull(envelope);
            Assert.True(envelope.Success);
            Assert.Equal(201, envelope.StatusCode);
            Assert.NotNull(envelope.Data);
            Assert.NotEqual(Guid.Empty, envelope.Data.Id);
            Assert.Equal("PostgreSQL", envelope.Data.Name);
            Assert.Equal(slug, envelope.Data.Slug);
            Assert.Equal("Relational database", envelope.Data.Description);
            Assert.NotNull(envelope.Data.Logo);
            Assert.StartsWith("https://localhost:59131/uploads/technologies/", envelope.Data.Logo);

            uploadedLogoUrl = envelope.Data.Logo;
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(uploadedLogoUrl))
            {
                using var scope = factory.Services.CreateScope();
                var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();
                await storage.DeleteAsync(uploadedLogoUrl);
            }
        }
    }

    [Fact]
    public async Task Create_WithDuplicateSlugForSameUser_Returns409Conflict()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        var slug = $"dup-tech-{Guid.NewGuid():N}";

        var first = await SendCreateTechnologyAsync(token, "Tech One", slug);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        // Act
        var second = await SendCreateTechnologyAsync(token, "Tech Two", slug);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var envelope = await second.Content.ReadFromJsonAsync<ApiResponse<object?>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.Success);
        Assert.Equal(409, envelope.StatusCode);
        Assert.Equal("A technology with this slug already exists.", envelope.Message);
    }

    [Fact]
    public async Task Create_WithInvalidData_Returns400BadRequestWithFirstErrorMessage()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();

        // Act
        var response = await SendCreateTechnologyAsync(token, "DotNet", "INVALID SLUG!");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<object?>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.Success);
        Assert.Equal(400, envelope.StatusCode);
        Assert.Equal("Slug must contain lowercase letters, numbers, and hyphens only.", envelope.Message);
    }

    [Fact]
    public async Task GetAll_WithoutToken_Returns401Unauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/technologies");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithValidToken_ReturnsOnlyUsersTechnologiesOrderedByName()
    {
        // Arrange
        var user1Token = await RegisterAndGetTokenAsync();
        var user2Token = await RegisterAndGetTokenAsync();

        await SendCreateTechnologyAsync(user1Token, "Zustand", $"z-{Guid.NewGuid():N}");
        await SendCreateTechnologyAsync(user1Token, "Angular", $"a-{Guid.NewGuid():N}");
        await SendCreateTechnologyAsync(user2Token, "Other Tech", $"o-{Guid.NewGuid():N}");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/technologies");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user1Token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<List<TechnologyResponse>>>();
        Assert.NotNull(envelope?.Data);
        Assert.True(envelope.Success);
        Assert.Equal(200, envelope.StatusCode);
        Assert.Equal(2, envelope.Data.Count);
        Assert.Equal("Angular", envelope.Data[0].Name);
        Assert.Equal("Zustand", envelope.Data[1].Name);
    }

    [Fact]
    public async Task GetById_WhenTechnologyExistsForUser_Returns200OK()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        var slug = $"get-{Guid.NewGuid():N}";
        var createResponse = await SendCreateTechnologyAsync(token, "Docker", slug);
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<TechnologyResponse>>())!.Data!;

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/technologies/{created.Id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<TechnologyResponse>>();
        Assert.NotNull(envelope?.Data);
        Assert.Equal(created.Id, envelope.Data.Id);
        Assert.Equal("Docker", envelope.Data.Name);
        Assert.Equal(slug, envelope.Data.Slug);
    }

    [Fact]
    public async Task GetById_WhenTechnologyBelongsToAnotherUser_Returns404NotFound()
    {
        // Arrange
        var ownerToken = await RegisterAndGetTokenAsync();
        var otherToken = await RegisterAndGetTokenAsync();
        var createResponse = await SendCreateTechnologyAsync(ownerToken, "Docker", $"priv-{Guid.NewGuid():N}");
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<TechnologyResponse>>())!.Data!;

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/technologies/{created.Id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithValidData_Returns200OKAndUpdatedTechnology()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        var createResponse = await SendCreateTechnologyAsync(token, "Old Tech", $"old-{Guid.NewGuid():N}");
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<TechnologyResponse>>())!.Data!;
        var updatedSlug = $"upd-{Guid.NewGuid():N}";

        // Act
        var response = await SendUpdateTechnologyAsync(token, created.Id, "New Tech", updatedSlug, "Updated description");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<TechnologyResponse>>();
        Assert.NotNull(envelope?.Data);
        Assert.True(envelope.Success);
        Assert.Equal(200, envelope.StatusCode);
        Assert.Equal(created.Id, envelope.Data.Id);
        Assert.Equal("New Tech", envelope.Data.Name);
        Assert.Equal(updatedSlug, envelope.Data.Slug);
        Assert.Equal("Updated description", envelope.Data.Description);
    }

    [Fact]
    public async Task Update_WithDuplicateSlug_Returns409Conflict()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        var firstSlug = $"t1-{Guid.NewGuid():N}";
        await SendCreateTechnologyAsync(token, "Tech One", firstSlug);
        var secondResponse = await SendCreateTechnologyAsync(token, "Tech Two", $"t2-{Guid.NewGuid():N}");
        var second = (await secondResponse.Content.ReadFromJsonAsync<ApiResponse<TechnologyResponse>>())!.Data!;

        // Act
        var response = await SendUpdateTechnologyAsync(token, second.Id, "Tech Two", firstSlug);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenTechnologyExistsForUser_Returns200OKAndRemovesTechnology()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        var createResponse = await SendCreateTechnologyAsync(token, "Delete Tech", $"del-{Guid.NewGuid():N}");
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<TechnologyResponse>>())!.Data!;

        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/technologies/{created.Id}");
        deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var deleteResponse = await _client.SendAsync(deleteRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        var envelope = await deleteResponse.Content.ReadFromJsonAsync<ApiResponse<object?>>();
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.Equal(200, envelope.StatusCode);
        Assert.Equal("Technology deleted successfully.", envelope.Message);

        var getRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/technologies/{created.Id}");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var getResponse = await _client.SendAsync(getRequest);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private async Task<string> RegisterAndGetTokenAsync()
    {
        var email = $"tech_user_{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest("Tech", "Owner", email, "Password@123");
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        regResponse.EnsureSuccessStatusCode();

        var auth = await regResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        Assert.NotNull(auth?.Data);
        return auth.Data.AccessToken;
    }

    private Task<HttpResponseMessage> SendCreateTechnologyAsync(
        string token,
        string name,
        string slug,
        string? description = null) =>
        SendTechnologyRequestAsync(HttpMethod.Post, "/api/technologies", token, name, slug, description);

    private Task<HttpResponseMessage> SendUpdateTechnologyAsync(
        string token,
        Guid id,
        string name,
        string slug,
        string? description = null) =>
        SendTechnologyRequestAsync(HttpMethod.Put, $"/api/technologies/{id}", token, name, slug, description);

    private async Task<HttpResponseMessage> SendTechnologyRequestAsync(
        HttpMethod method,
        string url,
        string token,
        string name,
        string slug,
        string? description = null)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(name), "Name");
        content.Add(new StringContent(slug), "Slug");
        if (description is not null)
        {
            content.Add(new StringContent(description), "Description");
        }

        var request = new HttpRequestMessage(method, url)
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await _client.SendAsync(request);
    }
}

