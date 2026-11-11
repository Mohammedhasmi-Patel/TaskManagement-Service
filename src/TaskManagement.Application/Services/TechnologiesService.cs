using FluentValidation;
using Microsoft.Extensions.Options;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Common.Options;
using TaskManagement.Application.DTOs.Technologies;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using ValidationException = TaskManagement.Application.Common.Exceptions.ValidationException;

namespace TaskManagement.Application.Services;

public class TechnologiesService(
    ITechnologyRepository technologyRepository,
    ICurrentUserService currentUserService,
    IStorageService storageService,
    IValidator<CreateTechnologyRequest> createValidator,
    IValidator<UpdateTechnologyRequest> updateValidator,
    IOptions<AppOptions> appOptions) : ITechnologiesService
{
    private readonly ITechnologyRepository _technologyRepository = technologyRepository;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IStorageService _storageService = storageService;
    private readonly IValidator<CreateTechnologyRequest> _createValidator = createValidator;
    private readonly IValidator<UpdateTechnologyRequest> _updateValidator = updateValidator;
    private readonly AppOptions _appOptions = appOptions.Value;

    public async Task<TechnologyResponse> CreateAsync(CreateTechnologyRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var userId = GetAuthenticatedUserId();

        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        var slugExists = await _technologyRepository.ExistsBySlugAsync(userId, normalizedSlug, cancellationToken: cancellationToken);
        if (slugExists)
        {
            throw new ConflictException("A technology with this slug already exists.");
        }

        string? logoUrl = null;
        if (request.Logo is not null)
        {
            var uploadResult = await _storageService.UploadAsync(request.Logo, "technologies", cancellationToken);
            logoUrl = uploadResult.FileUrl;
        }

        try
        {
            var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description;

            var technology = Technology.Create(
                userId: userId,
                name: request.Name,
                slug: normalizedSlug,
                description: description,
                logo: logoUrl);

            await _technologyRepository.AddAsync(technology, cancellationToken);
            await _technologyRepository.SaveChangesAsync(cancellationToken);

            return MapToResponse(technology);
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

    public async Task<IEnumerable<TechnologyResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var userId = GetAuthenticatedUserId();
        var technologies = await _technologyRepository.GetAllByUserIdAsync(userId, cancellationToken);

        return technologies.Select(MapToResponse);
    }

    public async Task<TechnologyResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetAuthenticatedUserId();
        var technology = await _technologyRepository.GetByIdAndUserIdAsync(id, userId, trackChanges: false, cancellationToken);
        if (technology is null)
        {
            throw new NotFoundException(nameof(Technology), id);
        }

        return MapToResponse(technology);
    }

    public async Task<TechnologyResponse> UpdateAsync(Guid id, UpdateTechnologyRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var userId = GetAuthenticatedUserId();
        var technology = await _technologyRepository.GetByIdAndUserIdAsync(id, userId, trackChanges: true, cancellationToken);
        if (technology is null)
        {
            throw new NotFoundException(nameof(Technology), id);
        }

        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        var slugExists = await _technologyRepository.ExistsBySlugAsync(userId, normalizedSlug, excludeTechnologyId: id, cancellationToken);
        if (slugExists)
        {
            throw new ConflictException("A technology with this slug already exists.");
        }

        var oldLogoUrl = technology.Logo;
        string? newLogoUrl = null;
        if (request.Logo is not null)
        {
            var uploadResult = await _storageService.UploadAsync(request.Logo, "technologies", cancellationToken);
            newLogoUrl = uploadResult.FileUrl;
        }

        try
        {
            var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description;

            technology.UpdateDetails(
                name: request.Name,
                slug: normalizedSlug,
                description: description,
                logo: newLogoUrl ?? oldLogoUrl);

            await _technologyRepository.SaveChangesAsync(cancellationToken);
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

        return MapToResponse(technology);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetAuthenticatedUserId();
        var technology = await _technologyRepository.GetByIdAndUserIdAsync(id, userId, trackChanges: true, cancellationToken);
        if (technology is null)
        {
            throw new NotFoundException(nameof(Technology), id);
        }

        var logoUrl = technology.Logo;

        await _technologyRepository.DeleteAsync(technology, cancellationToken);
        await _technologyRepository.SaveChangesAsync(cancellationToken);

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

    private TechnologyResponse MapToResponse(Technology technology)
    {
        return new TechnologyResponse(
            technology.Id,
            technology.Name,
            technology.Slug,
            technology.Description,
            _appOptions.BuildFileUrl(technology.Logo));
    }
}
