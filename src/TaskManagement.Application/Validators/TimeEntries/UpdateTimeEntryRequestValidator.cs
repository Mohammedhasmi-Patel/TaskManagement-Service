using FluentValidation;
using TaskManagement.Application.DTOs.TimeEntries;

namespace TaskManagement.Application.Validators.TimeEntries;

public class UpdateTimeEntryRequestValidator : AbstractValidator<UpdateTimeEntryRequest>
{
    public UpdateTimeEntryRequestValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty()
            .WithMessage("Task is required.");

        RuleFor(x => x.StartTime)
            .NotEmpty()
            .WithMessage("Start time is required.");

        RuleFor(x => x.Description)
            .MaximumLength(1000);

        RuleFor(x => x.DurationSeconds)
            .GreaterThanOrEqualTo(0)
            .When(x => x.DurationSeconds.HasValue)
            .WithMessage("Duration seconds cannot be negative.");

        RuleFor(x => x.EndTime)
            .Must((request, endTime) => !endTime.HasValue || endTime.Value >= request.StartTime)
            .WithMessage("End time must be on or after start time.");
    }
}

