using FluentValidation;
using TaskManagement.Application.DTOs.Technologies;

namespace TaskManagement.Application.Validators.Technologies;

public class CreateTechnologyRequestValidator : AbstractValidator<CreateTechnologyRequest>
{
    public CreateTechnologyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(120)
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug must contain lowercase letters, numbers, and hyphens only.");

        RuleFor(x => x.Description)
            .MaximumLength(1000);

        RuleFor(x => x.Logo)
            .Must(file => file == null || file.Length <= 2 * 1024 * 1024)
            .WithMessage("Logo size cannot exceed 2 MB.");

        RuleFor(x => x.Logo)
            .Must(file => file == null ||
                         new[] { ".jpg", ".jpeg", ".png", ".webp" }
                         .Contains(Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? string.Empty))
            .WithMessage("Only JPG, JPEG, PNG, and WEBP files are allowed.");
    }
}

