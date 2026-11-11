using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Auth;
using TaskManagement.Application.DTOs.Companies;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.IntegrationTests.Common;
using Xunit;

namespace TaskManagement.IntegrationTests.Controllers;

public class CompaniesControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_WithoutToken_Returns401Unauthorized()
    {
        // Arrange
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("Acme Corp"), "Name");
        content.Add(new StringContent("acme-corp"), "Slug");

        // Act
        var response = await _client.PostAsync("/api/companies", content);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<object?>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.Success);
        Assert.Equal(401, envelope.StatusCode);
        Assert.Null(envelope.Data);
    }

    [Fact]
    public async Task Create_WithValidData_Returns201CreatedAndCompany()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        var slug = $"acme-{Guid.NewGuid():N}";

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("Acme Corp"), "Name");
        content.Add(new StringContent(slug), "Slug");
        content.Add(new StringContent("Company description"), "Description");
        content.Add(new StringContent("https://acme.example.com"), "Website");
        content.Add(new StringContent("true"), "IsDefault");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/companies")
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<CompanyResponse>>();
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.Equal(201, envelope.StatusCode);
        Assert.NotNull(envelope.Data);
        Assert.NotEqual(Guid.Empty, envelope.Data.Id);
        Assert.Equal("Acme Corp", envelope.Data.Name);
        Assert.Equal(slug, envelope.Data.Slug);
        Assert.Equal("https://acme.example.com", envelope.Data.Website);
        Assert.True(envelope.Data.IsDefault);
    }

    [Fact]
    public async Task Create_WithDuplicateSlugForSameUser_Returns409Conflict()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        var slug = $"dup-{Guid.NewGuid():N}";

        var firstResponse = await SendCreateCompanyAsync(token, "First Co", slug);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        // Act
        var secondResponse = await SendCreateCompanyAsync(token, "Second Co", slug);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        var envelope = await secondResponse.Content.ReadFromJsonAsync<ApiResponse<object?>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.Success);
        Assert.Equal(409, envelope.StatusCode);
        Assert.Null(envelope.Data);
        Assert.Equal("A company with this slug already exists.", envelope.Message);
    }

    [Fact]
    public async Task Create_WithInvalidData_Returns400BadRequestWithFirstErrorMessage()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();

        // Act
        var response = await SendCreateCompanyAsync(token, "Valid Name", "INVALID SLUG!");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<object?>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.Success);
        Assert.Equal(400, envelope.StatusCode);
        Assert.Null(envelope.Data);
        Assert.Equal("Slug must contain lowercase letters, numbers, and hyphens only.", envelope.Message);
    }

    [Fact]
    public async Task Create_WithIsDefaultTrue_UnsetsPreviousDefaultCompany()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        var firstSlug = $"first-{Guid.NewGuid():N}";
        var secondSlug = $"second-{Guid.NewGuid():N}";

        var firstResponse = await SendCreateCompanyAsync(token, "First Co", firstSlug, isDefault: true);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        var firstEnvelope = await firstResponse.Content.ReadFromJsonAsync<ApiResponse<CompanyResponse>>();
        Assert.NotNull(firstEnvelope?.Data);
        Assert.True(firstEnvelope.Data.IsDefault);

        // Act
        var secondResponse = await SendCreateCompanyAsync(token, "Second Co", secondSlug, isDefault: true);

        // Assert
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);
        var secondEnvelope = await secondResponse.Content.ReadFromJsonAsync<ApiResponse<CompanyResponse>>();
        Assert.NotNull(secondEnvelope?.Data);
        Assert.True(secondEnvelope.Data.IsDefault);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var savedFirst = await dbContext.Companies.FirstOrDefaultAsync(c => c.Id == firstEnvelope.Data.Id);
        var savedSecond = await dbContext.Companies.FirstOrDefaultAsync(c => c.Id == secondEnvelope.Data.Id);

        Assert.NotNull(savedFirst);
        Assert.NotNull(savedSecond);
        Assert.False(savedFirst.IsDefault);
        Assert.True(savedSecond.IsDefault);
    }

    [Fact]
    public async Task GetAll_WithoutToken_Returns401Unauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/companies");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithValidToken_ReturnsOnlyUsersCompaniesWithDefaultFirst()
    {
        // Arrange
        var user1Token = await RegisterAndGetTokenAsync();
        var user2Token = await RegisterAndGetTokenAsync();

        var slugA = $"alpha-{Guid.NewGuid():N}";
        var slugB = $"beta-{Guid.NewGuid():N}";
        var slugOther = $"other-{Guid.NewGuid():N}";

        await SendCreateCompanyAsync(user1Token, "Zeta Co", slugA, isDefault: false);
        await SendCreateCompanyAsync(user1Token, "Beta Default Co", slugB, isDefault: true);
        await SendCreateCompanyAsync(user2Token, "Other User Co", slugOther, isDefault: true);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/companies");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user1Token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<List<CompanyResponse>>>();
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.Equal(200, envelope.StatusCode);
        Assert.NotNull(envelope.Data);
        Assert.Equal(2, envelope.Data.Count);
        Assert.Equal("Beta Default Co", envelope.Data[0].Name);
        Assert.True(envelope.Data[0].IsDefault);
        Assert.Equal("Zeta Co", envelope.Data[1].Name);
        Assert.False(envelope.Data[1].IsDefault);
    }

    [Fact]
    public async Task GetById_WithoutToken_Returns401Unauthorized()
    {
        // Act
        var response = await _client.GetAsync($"/api/companies/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WhenCompanyExistsForUser_Returns200OKAndCompany()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        var slug = $"getbyid-{Guid.NewGuid():N}";
        var createResponse = await SendCreateCompanyAsync(token, "Target Co", slug, isDefault: true);
        createResponse.EnsureSuccessStatusCode();
        var createdEnvelope = await createResponse.Content.ReadFromJsonAsync<ApiResponse<CompanyResponse>>();
        Assert.NotNull(createdEnvelope?.Data);
        var created = createdEnvelope.Data;

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/companies/{created.Id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetchedEnvelope = await response.Content.ReadFromJsonAsync<ApiResponse<CompanyResponse>>();
        Assert.NotNull(fetchedEnvelope);
        Assert.True(fetchedEnvelope.Success);
        Assert.Equal(200, fetchedEnvelope.StatusCode);
        Assert.NotNull(fetchedEnvelope.Data);
        Assert.Equal(created.Id, fetchedEnvelope.Data.Id);
        Assert.Equal("Target Co", fetchedEnvelope.Data.Name);
        Assert.Equal(slug, fetchedEnvelope.Data.Slug);
        Assert.True(fetchedEnvelope.Data.IsDefault);
    }

    [Fact]
    public async Task GetById_WhenCompanyDoesNotExist_Returns404NotFound()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/companies/{Guid.NewGuid()}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<object?>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.Success);
        Assert.Equal(404, envelope.StatusCode);
        Assert.Null(envelope.Data);
        Assert.False(string.IsNullOrWhiteSpace(envelope.Message));
    }

    [Fact]
    public async Task GetById_WhenCompanyBelongsToAnotherUser_Returns404NotFound()
    {
        // Arrange
        var ownerToken = await RegisterAndGetTokenAsync();
        var otherToken = await RegisterAndGetTokenAsync();
        var createResponse = await SendCreateCompanyAsync(ownerToken, "Private Co", $"priv-{Guid.NewGuid():N}");
        createResponse.EnsureSuccessStatusCode();
        var createdEnvelope = await createResponse.Content.ReadFromJsonAsync<ApiResponse<CompanyResponse>>();
        Assert.NotNull(createdEnvelope?.Data);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/companies/{createdEnvelope.Data.Id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<object?>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.Success);
        Assert.Equal(404, envelope.StatusCode);
        Assert.Null(envelope.Data);
    }

    private async Task<string> RegisterAndGetTokenAsync()
    {
        var email = $"company_user_{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest("Company", "Owner", email, "Password@123");
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        regResponse.EnsureSuccessStatusCode();

        var auth = await regResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        Assert.NotNull(auth?.Data);
        return auth.Data.AccessToken;
    }

    private async Task<HttpResponseMessage> SendCreateCompanyAsync(
        string token,
        string name,
        string slug,
        bool isDefault = false)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(name), "Name");
        content.Add(new StringContent(slug), "Slug");
        content.Add(new StringContent(isDefault.ToString().ToLowerInvariant()), "IsDefault");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/companies")
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await _client.SendAsync(request);
    }
}
