using TaskManagement.Application.DTOs.TimeEntries;
using TaskManagement.Application.Validators.TimeEntries;
using Xunit;

namespace TaskManagement.UnitTests.Validation;

public class UpdateTimeEntryRequestValidatorTests
{
    private readonly UpdateTimeEntryRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidRequest_ShouldPass()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow.AddHours(-1);
        var request = new UpdateTimeEntryRequest
        {
            TaskId = Guid.NewGuid(),
            StartTime = start,
            EndTime = start.AddMinutes(30),
            Description = "Updated entry"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_WhenEndTimeIsBeforeStartTime_ShouldFail()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow;
        var request = new UpdateTimeEntryRequest
        {
            TaskId = Guid.NewGuid(),
            StartTime = start,
            EndTime = start.AddSeconds(-1)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTimeEntryRequest.EndTime));
    }
}

