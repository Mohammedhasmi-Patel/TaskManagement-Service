using Microsoft.AspNetCore.Http;
using TaskManagement.Application.DTOs.Companies;
using TaskManagement.Application.Validators.Companies;
using Xunit;

namespace TaskManagement.UnitTests.Validation;

public class CreateCompanyRequestValidatorTests
{
    private readonly CreateCompanyRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidRequest_ShouldPass()
    {
        // Arrange
        var request = new CreateCompanyRequest
        {
            Name = "Acme Corp",
            Slug = "acme-corp",
            Description = "Software engineering company",
            Website = "https://acme.example.com",
            IsDefault = true
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("", "acme-corp", "https://acme.example.com")]
    [InlineData("Acme", "", "https://acme.example.com")]
    [InlineData("Acme", "Invalid Slug!", "https://acme.example.com")]
    [InlineData("Acme", "acme--corp", "https://acme.example.com")]
    [InlineData("Acme", "acme-corp", "ftp://acme.example.com")]
    [InlineData("Acme", "acme-corp", "not-a-url")]
    public async Task Validate_WithInvalidRequest_ShouldFail(
        string name, string slug, string website)
    {
        // Arrange
        var request = new CreateCompanyRequest
        {
            Name = name,
            Slug = slug,
            Website = website
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
        var request = new CreateCompanyRequest
        {
            Name = "Acme Corp",
            Slug = "acme-corp",
            Logo = file
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCompanyRequest.Logo));
    }

    [Fact]
    public async Task Validate_WithUnsupportedLogoExtension_ShouldFail()
    {
        // Arrange
        using var stream = new MemoryStream([1, 2, 3]);
        var file = new FormFile(stream, 0, stream.Length, "Logo", "logo.gif");
        var request = new CreateCompanyRequest
        {
            Name = "Acme Corp",
            Slug = "acme-corp",
            Logo = file
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCompanyRequest.Logo));
    }
}

