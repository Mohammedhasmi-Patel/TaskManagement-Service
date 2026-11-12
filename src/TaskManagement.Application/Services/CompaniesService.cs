using FluentValidation;
using Microsoft.Extensions.Options;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Common.Options;
using TaskManagement.Application.DTOs.Companies;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using ValidationException = TaskManagement.Application.Common.Exceptions.ValidationException;

namespace TaskManagement.Application.Services;

public class CompaniesService(
    ICompanyRepository companyRepository,
    ICurrentUserService currentUserService,
    IStorageService storageService,
    IValidator<CreateCompanyRequest> createValidator,
    IValidator<UpdateCompanyRequest> updateValidator,
    IOptions<AppOptions> appOptions) : ICompaniesService
{
    private readonly ICompanyRepository _companyRepository = companyRepository;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IStorageService _storageService = storageService;
    private readonly IValidator<CreateCompanyRequest> _createValidator = createValidator;
    private readonly IValidator<UpdateCompanyRequest> _updateValidator = updateValidator;
    private readonly AppOptions _appOptions = appOptions.Value;

    public async Task<CompanyResponse> CreateAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var userId = GetAuthenticatedUserId();

        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        var slugExists = await _companyRepository.ExistsBySlugAsync(userId, normalizedSlug, cancellationToken: cancellationToken);
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
                await _companyRepository.ClearDefaultForUserAsync(userId, cancellationToken: cancellationToken);
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

            return MapToResponse(company);
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

    public async Task<IEnumerable<CompanyResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var userId = GetAuthenticatedUserId();
        var companies = await _companyRepository.GetAllByUserIdAsync(userId, cancellationToken);

        return companies.Select(MapToResponse);
    }

    public async Task<CompanyResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetAuthenticatedUserId();
        var company = await _companyRepository.GetByIdAndUserIdAsync(id, userId, trackChanges: false, cancellationToken);
        if (company is null)
        {
            throw new NotFoundException(nameof(Company), id);
        }

        return MapToResponse(company);
    }

    public async Task<CompanyResponse> UpdateAsync(Guid id, UpdateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var userId = GetAuthenticatedUserId();
        var company = await _companyRepository.GetByIdAndUserIdAsync(id, userId, trackChanges: true, cancellationToken) ?? throw new NotFoundException(nameof(Company), id);
        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        var slugExists = await _companyRepository.ExistsBySlugAsync(userId, normalizedSlug, excludeCompanyId: id, cancellationToken);
        if (slugExists)
        {
            throw new ConflictException("A company with this slug already exists.");
        }

        var oldLogoUrl = company.Logo;
        string? newLogoUrl = null;
        if (request.Logo is not null)
        {
            var uploadResult = await _storageService.UploadAsync(request.Logo, "companies", cancellationToken);
            newLogoUrl = uploadResult.FileUrl;
        }

        try
        {
            if (request.IsDefault)
            {
                await _companyRepository.ClearDefaultForUserAsync(userId, excludeCompanyId: id, cancellationToken);
            }

            var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description;
            var website = string.IsNullOrWhiteSpace(request.Website) ? null : request.Website;

            company.UpdateDetails(
                name: request.Name,
                slug: normalizedSlug,
                description: description,
                logo: newLogoUrl ?? oldLogoUrl,
                website: website);

            company.SetDefault(request.IsDefault);

            await _companyRepository.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(newLogoUrl))
            {
                await _storageService.DeleteAsync(newLogoUrl, CancellationToken.None);
            }

            throw;
        }

        if (newLogoUrl is not null && !string.IsNullOrWhiteSpace(oldLogoUrl))
        {
            await _storageService.DeleteAsync(oldLogoUrl, CancellationToken.None);
        }

        return MapToResponse(company);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetAuthenticatedUserId();
        var company = await _companyRepository.GetByIdAndUserIdAsync(id, userId, trackChanges: true, cancellationToken);
        if (company is null)
        {
            throw new NotFoundException(nameof(Company), id);
        }

        var logoUrl = company.Logo;

        await _companyRepository.DeleteAsync(company, cancellationToken);
        await _companyRepository.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(logoUrl))
        {
            await _storageService.DeleteAsync(logoUrl, CancellationToken.None);
        }
    }

    private Guid GetAuthenticatedUserId()
    {
        return _currentUserService.UserId
               ?? throw new UnauthorizedException("User is not authenticated.");
    }

    private CompanyResponse MapToResponse(Company company)
    {
        return new CompanyResponse(
            Id: company.Id,
            Name: company.Name,
            Slug: company.Slug,
            Logo: _appOptions.BuildFileUrl(company.Logo),
            Website: company.Website,
            Description: company.Description,
            IsDefault: company.IsDefault);
    }
}
