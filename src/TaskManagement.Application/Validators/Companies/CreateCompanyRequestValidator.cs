using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Application.DTOs.Companies;

namespace TaskManagement.Application.Validators.Companies;

public class CreateCompanyRequestValidator : AbstractValidator<CreateCompanyRequest>
{
    public CreateCompanyRequestValidator()
    {
        
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(180)
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug must contain lowercase letters, numbers, and hyphens only.");

        RuleFor(x => x.Description)
            .MaximumLength(1000);

        RuleFor(x => x.Website)
            .Must(url => string.IsNullOrWhiteSpace(url) ||
                         Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                         (uri.Scheme == Uri.UriSchemeHttp ||
                          uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("Please enter a valid website URL.");

        RuleFor(x => x.Logo)
            .Must(file => file == null || file.Length <= 2 * 1024 * 1024)
            .WithMessage("Logo size cannot exceed 2 MB.");

        RuleFor(x => x.Logo)
            .Must(file => file == null ||
                         new[] { ".jpg", ".jpeg", ".png", ".webp" }
                         .Contains(Path.GetExtension(file.FileName).ToLowerInvariant()))
            .WithMessage("Only JPG, JPEG, PNG, and WEBP files are allowed.");
    }

}
