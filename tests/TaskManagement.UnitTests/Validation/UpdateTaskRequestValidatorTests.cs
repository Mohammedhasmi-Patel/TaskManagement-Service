using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Application.Validators.Tasks;
using TaskManagement.Domain.Enums;
using Xunit;

namespace TaskManagement.UnitTests.Validation;

public class UpdateTaskRequestValidatorTests
{
    private readonly UpdateTaskRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidRequest_ShouldPass()
    {
        // Arrange
        var request = new UpdateTaskRequest
        {
            ProjectId = Guid.NewGuid(),
            Title = "Updated Task",
            Description = "Updated details",
            Status = TaskStatusEnum.Completed,
            Priority = TaskPriority.Critical,
            EstimatedHours = 2.0m
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_WithNegativeEstimatedHours_ShouldFail()
    {
        // Arrange
        var request = new UpdateTaskRequest
        {
            ProjectId = Guid.NewGuid(),
            Title = "Task Title",
            EstimatedHours = -0.5m
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTaskRequest.EstimatedHours));
    }
}

