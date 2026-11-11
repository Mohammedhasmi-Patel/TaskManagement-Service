using TaskManagement.Application.DTOs.Projects;
using TaskManagement.Application.Validators.Projects;
using TaskManagement.Domain.Enums;
using Xunit;

namespace TaskManagement.UnitTests.Validation;

public class UpdateProjectRequestValidatorTests
{
    private readonly UpdateProjectRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidRequest_ShouldPass()
    {
        // Arrange
        var request = new UpdateProjectRequest
        {
            CompanyId = Guid.NewGuid(),
            Name = "Task Tracker V2",
            Slug = "task-tracker-v2",
            Description = "Updated description",
            Status = ProjectStatus.Completed,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 15),
            IsActive = false
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenEndDateIsBeforeStartDate_ShouldFail()
    {
        // Arrange
        var request = new UpdateProjectRequest
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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateProjectRequest.EndDate));
    }
}

