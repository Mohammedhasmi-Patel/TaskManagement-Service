using Microsoft.AspNetCore.Http;
using TaskManagement.Application.DTOs.Companies;
using TaskManagement.Application.Validators.Companies;
using Xunit;

namespace TaskManagement.UnitTests.Validation;

public class UpdateCompanyRequestValidatorTests
{
    private readonly UpdateCompanyRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidRequest_ShouldPass()
    {
        // Arrange
        var request = new UpdateCompanyRequest
        {
            Name = "Updated Corp",
            Slug = "updated-corp",
            Description = "Updated engineering company",
            Website = "https://updated.example.com",
            IsDefault = true
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("", "updated-corp", "https://updated.example.com")]
    [InlineData("Updated", "", "https://updated.example.com")]
    [InlineData("Updated", "Invalid Slug!", "https://updated.example.com")]
    [InlineData("Updated", "updated-corp", "ftp://updated.example.com")]
    public async Task Validate_WithInvalidRequest_ShouldFail(
        string name, string slug, string website)
    {
        // Arrange
        var request = new UpdateCompanyRequest
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
    public async Task Validate_WithUnsupportedLogoExtension_ShouldFail()
    {
        // Arrange
        using var stream = new MemoryStream([1, 2, 3]);
        var file = new FormFile(stream, 0, stream.Length, "Logo", "logo.bmp");
        var request = new UpdateCompanyRequest
        {
            Name = "Updated Corp",
            Slug = "updated-corp",
            Logo = file
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateCompanyRequest.Logo));
    }
}

