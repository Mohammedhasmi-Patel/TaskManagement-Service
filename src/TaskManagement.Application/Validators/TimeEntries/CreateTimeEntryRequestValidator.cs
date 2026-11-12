using FluentValidation;
using TaskManagement.Application.DTOs.TimeEntries;

namespace TaskManagement.Application.Validators.TimeEntries;

public class CreateTimeEntryRequestValidator : AbstractValidator<CreateTimeEntryRequest>
{
    public CreateTimeEntryRequestValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty()
            .WithMessage("Task is required.");

        RuleFor(x => x.Description)
            .MaximumLength(1000);

        RuleFor(x => x.DurationSeconds)
            .GreaterThanOrEqualTo(0)
            .When(x => x.DurationSeconds.HasValue)
            .WithMessage("Duration seconds cannot be negative.");

        RuleFor(x => x.EndTime)
            .Must((request, endTime) => !endTime.HasValue || request.StartTime.HasValue)
            .WithMessage("Start time is required when end time is provided.")
            .Must((request, endTime) => !endTime.HasValue || !request.StartTime.HasValue || endTime.Value >= request.StartTime.Value)
            .WithMessage("End time must be on or after start time.");
    }
}

