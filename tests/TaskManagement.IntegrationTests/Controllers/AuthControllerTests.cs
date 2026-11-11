using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Auth;
using TaskManagement.IntegrationTests.Common;
using Xunit;

namespace TaskManagement.IntegrationTests.Controllers;

public class AuthControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_WithValidData_Returns201CreatedAndToken()
    {
        // Arrange
        var email = $"register_{Guid.NewGuid()}@example.com";
        var request = new RegisterRequest("Test", "User", email, "Password@123");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.Equal(201, envelope.StatusCode);
        Assert.NotNull(envelope.Data);
        Assert.NotEmpty(envelope.Data.AccessToken);
        Assert.Equal(email, envelope.Data.User.Email);
        Assert.Equal("Test", envelope.Data.User.FirstName);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409Conflict()
    {
        // Arrange
        var email = $"duplicate_{Guid.NewGuid()}@example.com";
        var request = new RegisterRequest("Test", "User", email, "Password@123");

        // First registration
        var firstResponse = await _client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        // Act - Second registration with same email
        var secondResponse = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        var envelope = await secondResponse.Content.ReadFromJsonAsync<ApiResponse<object?>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.Success);
        Assert.Equal(409, envelope.StatusCode);
        Assert.Null(envelope.Data);
        Assert.False(string.IsNullOrWhiteSpace(envelope.Message));
    }

    [Fact]
    public async Task Register_WithInvalidData_Returns400BadRequestWithFirstErrorMessage()
    {
        // Arrange
        var request = new RegisterRequest("", "", "invalid-email", "short");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<object?>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.Success);
        Assert.Equal(400, envelope.StatusCode);
        Assert.Null(envelope.Data);
        Assert.Equal("First name is required.", envelope.Message);
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200OKAndToken()
    {
        // Arrange - Register user first
        var email = $"login_{Guid.NewGuid()}@example.com";
        const string password = "Password@123";
        var registerRequest = new RegisterRequest("Login", "Tester", email, password);
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest(email, password);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.Equal(200, envelope.StatusCode);
        Assert.NotNull(envelope.Data);
        Assert.NotEmpty(envelope.Data.AccessToken);
        Assert.Equal(email, envelope.Data.User.Email);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_Returns401Unauthorized()
    {
        // Arrange
        var email = $"wrongpass_{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest("Login", "Tester", email, "Password@123");
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest(email, "WrongPassword@999");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<object?>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.Success);
        Assert.Equal(401, envelope.StatusCode);
        Assert.Null(envelope.Data);
        Assert.Equal("Invalid email or password.", envelope.Message);
    }

    [Fact]
    public async Task GetMe_WithValidJwtToken_Returns200OKAndUserProfile()
    {
        // Arrange - Register to get valid token
        var email = $"me_{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest("Me", "User", email, "Password@123");
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authEnvelope = await regResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        Assert.NotNull(authEnvelope?.Data);

        // Attach JWT
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authEnvelope.Data.AccessToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.Equal(200, envelope.StatusCode);
        Assert.NotNull(envelope.Data);
        Assert.Equal(email, envelope.Data.Email);
        Assert.Equal("Me", envelope.Data.FirstName);
    }

    [Fact]
    public async Task GetMe_WithoutToken_Returns401Unauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<object?>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.Success);
        Assert.Equal(401, envelope.StatusCode);
        Assert.Null(envelope.Data);
        Assert.False(string.IsNullOrWhiteSpace(envelope.Message));
    }

    [Fact]
    public async Task OptionsPreflight_WithOriginHeader_ReturnsCorsHeaders()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
