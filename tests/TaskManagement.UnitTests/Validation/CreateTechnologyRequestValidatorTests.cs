using Microsoft.AspNetCore.Http;
using TaskManagement.Application.DTOs.Technologies;
using TaskManagement.Application.Validators.Technologies;
using Xunit;

namespace TaskManagement.UnitTests.Validation;

public class CreateTechnologyRequestValidatorTests
{
    private readonly CreateTechnologyRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidRequest_ShouldPass()
    {
        // Arrange
        var request = new CreateTechnologyRequest
        {
            Name = ".NET 10",
            Slug = "dotnet-10",
            Description = "Backend framework"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("", "dotnet")]
    [InlineData("DotNet", "")]
    [InlineData("DotNet", "Invalid Slug!")]
    [InlineData("DotNet", "dot--net")]
    public async Task Validate_WithInvalidRequest_ShouldFail(string name, string slug)
    {
        // Arrange
        var request = new CreateTechnologyRequest
        {
            Name = name,
            Slug = slug
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WithOversizedLogo_ShouldFail()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[(2 * 1024 * 1024) + 1]);
        var file = new FormFile(stream, 0, stream.Length, "Logo", "logo.png");
        var request = new CreateTechnologyRequest
        {
            Name = "DotNet",
            Slug = "dotnet",
            Logo = file
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTechnologyRequest.Logo));
    }

    [Fact]
    public async Task Validate_WithUnsupportedLogoExtension_ShouldFail()
    {
        // Arrange
        using var stream = new MemoryStream([1, 2, 3]);
        var file = new FormFile(stream, 0, stream.Length, "Logo", "logo.gif");
        var request = new CreateTechnologyRequest
        {
            Name = "DotNet",
            Slug = "dotnet",
            Logo = file
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTechnologyRequest.Logo));
    }
}

