using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

        var created = await response.Content.ReadFromJsonAsync<CompanyResponse>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("Acme Corp", created.Name);
        Assert.Equal(slug, created.Slug);
        Assert.Equal("https://acme.example.com", created.Website);
        Assert.True(created.IsDefault);
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
    }

    [Fact]
    public async Task Create_WithInvalidData_Returns400BadRequest()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();

        // Act
        var response = await SendCreateCompanyAsync(token, "", "INVALID SLUG!");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
        var firstCompany = await firstResponse.Content.ReadFromJsonAsync<CompanyResponse>();
        Assert.NotNull(firstCompany);
        Assert.True(firstCompany.IsDefault);

        // Act
        var secondResponse = await SendCreateCompanyAsync(token, "Second Co", secondSlug, isDefault: true);

        // Assert
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);
        var secondCompany = await secondResponse.Content.ReadFromJsonAsync<CompanyResponse>();
        Assert.NotNull(secondCompany);
        Assert.True(secondCompany.IsDefault);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var savedFirst = await dbContext.Companies.FirstOrDefaultAsync(c => c.Id == firstCompany.Id);
        var savedSecond = await dbContext.Companies.FirstOrDefaultAsync(c => c.Id == secondCompany.Id);

        Assert.NotNull(savedFirst);
        Assert.NotNull(savedSecond);
        Assert.False(savedFirst.IsDefault);
        Assert.True(savedSecond.IsDefault);
    }

    private async Task<string> RegisterAndGetTokenAsync()
    {
        var email = $"company_user_{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest("Company", "Owner", email, "Password@123");
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        regResponse.EnsureSuccessStatusCode();

        var auth = await regResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        return auth.AccessToken;
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

