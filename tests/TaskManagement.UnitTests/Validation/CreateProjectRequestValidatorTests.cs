using TaskManagement.Application.DTOs.Projects;
using TaskManagement.Application.Validators.Projects;
using TaskManagement.Domain.Enums;
using Xunit;

namespace TaskManagement.UnitTests.Validation;

public class CreateProjectRequestValidatorTests
{
    private readonly CreateProjectRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidRequest_ShouldPass()
    {
        // Arrange
        var request = new CreateProjectRequest
        {
            CompanyId = Guid.NewGuid(),
            Name = "Task Tracker",
            Slug = "task-tracker",
            Description = "Internal tool",
            Status = ProjectStatus.InProgress,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 1)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_WithEmptyCompanyId_ShouldFail()
    {
        // Arrange
        var request = new CreateProjectRequest
        {
            CompanyId = Guid.Empty,
            Name = "Task Tracker",
            Slug = "task-tracker"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProjectRequest.CompanyId));
    }

    [Theory]
    [InlineData("", "task-tracker")]
    [InlineData("Task Tracker", "")]
    [InlineData("Task Tracker", "Invalid Slug!")]
    [InlineData("Task Tracker", "task--tracker")]
    public async Task Validate_WithInvalidNameOrSlug_ShouldFail(string name, string slug)
    {
        // Arrange
        var request = new CreateProjectRequest
        {
            CompanyId = Guid.NewGuid(),
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
    public async Task Validate_WhenEndDateIsBeforeStartDate_ShouldFail()
    {
        // Arrange
        var request = new CreateProjectRequest
        {
            CompanyId = Guid.NewGuid(),
            Name = "Task Tracker",
            Slug = "task-tracker",
            StartDate = new DateOnly(2026, 5, 1),
            EndDate = new DateOnly(2026, 4, 1)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProjectRequest.EndDate));
    }
}

