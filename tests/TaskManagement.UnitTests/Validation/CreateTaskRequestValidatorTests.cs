using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Application.Validators.Tasks;
using TaskManagement.Domain.Enums;
using Xunit;

namespace TaskManagement.UnitTests.Validation;

public class CreateTaskRequestValidatorTests
{
    private readonly CreateTaskRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidRequest_ShouldPass()
    {
        var request = new CreateTaskRequest
        {
            ProjectId = Guid.NewGuid(),
            Title = "Implement JWT Auth",
            Description = "Add login and register endpoints",
            Status = TaskStatusEnum.InProgress,
            Priority = TaskPriority.High,
            EstimatedHours = 4.5m
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_WithEmptyProjectId_ShouldFail()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            ProjectId = Guid.Empty,
            Title = "Task Title"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTaskRequest.ProjectId));
    }

    [Fact]
    public async Task Validate_WithEmptyTitle_ShouldFail()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            ProjectId = Guid.NewGuid(),
            Title = ""
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTaskRequest.Title));
    }

    [Fact]
    public async Task Validate_WithNegativeEstimatedHours_ShouldFail()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            ProjectId = Guid.NewGuid(),
            Title = "Task Title",
            EstimatedHours = -1m
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTaskRequest.EstimatedHours));
    }
}

