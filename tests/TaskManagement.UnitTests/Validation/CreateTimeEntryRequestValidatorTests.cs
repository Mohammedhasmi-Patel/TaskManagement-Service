using TaskManagement.Application.DTOs.TimeEntries;
using TaskManagement.Application.Validators.TimeEntries;
using Xunit;

namespace TaskManagement.UnitTests.Validation;

public class CreateTimeEntryRequestValidatorTests
{
    private readonly CreateTimeEntryRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WithStartTimeOnly_ShouldPass()
    {
        // Arrange
        var request = new CreateTimeEntryRequest
        {
            TaskId = Guid.NewGuid(),
            StartTime = DateTimeOffset.UtcNow,
            Description = "Started timer"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithEndTimeWithoutStartTime_ShouldFail()
    {
        // Arrange
        var request = new CreateTimeEntryRequest
        {
            TaskId = Guid.NewGuid(),
            EndTime = DateTimeOffset.UtcNow
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTimeEntryRequest.EndTime));
    }

    [Fact]
    public async Task Validate_WhenEndTimeIsBeforeStartTime_ShouldFail()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var request = new CreateTimeEntryRequest
        {
            TaskId = Guid.NewGuid(),
            StartTime = now,
            EndTime = now.AddMinutes(-5)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTimeEntryRequest.EndTime));
    }

    [Fact]
    public async Task Validate_WithNegativeDuration_ShouldFail()
    {
        // Arrange
        var request = new CreateTimeEntryRequest
        {
            TaskId = Guid.NewGuid(),
            StartTime = DateTimeOffset.UtcNow,
            DurationSeconds = -10
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTimeEntryRequest.DurationSeconds));
    }
}

