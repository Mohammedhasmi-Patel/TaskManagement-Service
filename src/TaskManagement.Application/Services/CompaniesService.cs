using FluentValidation;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.DTOs.Companies;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using ValidationException = TaskManagement.Application.Common.Exceptions.ValidationException;

namespace TaskManagement.Application.Services;

public class CompaniesService(
    ICompanyRepository companyRepository,
    ICurrentUserService currentUserService,
    IStorageService storageService,
    IValidator<CreateCompanyRequest> createValidator) : ICompaniesService
{
    private readonly ICompanyRepository _companyRepository = companyRepository;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IStorageService _storageService = storageService;
    private readonly IValidator<CreateCompanyRequest> _createValidator = createValidator;

    public async Task<CompanyResponse> CreateAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var userId = _currentUserService.UserId
                     ?? throw new UnauthorizedException("User is not authenticated.");

        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        var slugExists = await _companyRepository.ExistsBySlugAsync(userId, normalizedSlug, cancellationToken);
        if (slugExists)
        {
            throw new ConflictException("A company with this slug already exists.");
        }

        string? logoUrl = null;
        if (request.Logo is not null)
        {
            var uploadResult = await _storageService.UploadAsync(request.Logo, "companies", cancellationToken);
            logoUrl = uploadResult.FileUrl;
        }

        try
        {
            if (request.IsDefault)
            {
                await _companyRepository.ClearDefaultForUserAsync(userId, cancellationToken);
            }

            var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description;
            var website = string.IsNullOrWhiteSpace(request.Website) ? null : request.Website;

            var company = Company.Create(
                userId: userId,
                name: request.Name,
                slug: normalizedSlug,
                description: description,
                logo: logoUrl,
                website: website,
                isDefault: request.IsDefault);

            await _companyRepository.AddAsync(company, cancellationToken);
            await _companyRepository.SaveChangesAsync(cancellationToken);

            return new CompanyResponse(
                company.Id,
                company.Name,
                company.Slug,
                company.Logo,
                company.Website,
                company.IsDefault);
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(logoUrl))
            {
                await _storageService.DeleteAsync(logoUrl, CancellationToken.None);
            }

            throw;
        }
    }
}
