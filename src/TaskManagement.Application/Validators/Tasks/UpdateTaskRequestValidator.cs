using FluentValidation;
using TaskManagement.Application.DTOs.Tasks;

namespace TaskManagement.Application.Validators.Tasks;

public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithMessage("Project is required.");

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(2000);

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Invalid task status.");

        RuleFor(x => x.Priority)
            .IsInEnum()
            .WithMessage("Invalid task priority.");

        RuleFor(x => x.EstimatedHours)
            .GreaterThanOrEqualTo(0)
            .When(x => x.EstimatedHours.HasValue)
            .WithMessage("Estimated hours cannot be negative.");
    }
}

