using FluentValidation;
using Microsoft.Extensions.Options;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Common.Options;
using TaskManagement.Application.DTOs.Projects;
using TaskManagement.Application.DTOs.Technologies;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using ValidationException = TaskManagement.Application.Common.Exceptions.ValidationException;

namespace TaskManagement.Application.Services;

public class ProjectsService(
    IProjectRepository projectRepository,
    ICompanyRepository companyRepository,
    ITechnologyRepository technologyRepository,
    ICurrentUserService currentUserService,
    IValidator<CreateProjectRequest> createValidator,
    IValidator<UpdateProjectRequest> updateValidator,
    IOptions<AppOptions> appOptions) : IProjectsService
{
    private readonly IProjectRepository _projectRepository = projectRepository;
    private readonly ICompanyRepository _companyRepository = companyRepository;
    private readonly ITechnologyRepository _technologyRepository = technologyRepository;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IValidator<CreateProjectRequest> _createValidator = createValidator;
    private readonly IValidator<UpdateProjectRequest> _updateValidator = updateValidator;
    private readonly AppOptions _appOptions = appOptions.Value;

    public async Task<ProjectResponse> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var userId = GetAuthenticatedUserId();

        var company = await _companyRepository.GetByIdAndUserIdAsync(request.CompanyId, userId, trackChanges: false, cancellationToken);
        if (company is null)
        {
            throw new NotFoundException(nameof(Company), request.CompanyId);
        }

        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        var slugExists = await _projectRepository.ExistsBySlugAsync(request.CompanyId, normalizedSlug, cancellationToken: cancellationToken);
        if (slugExists)
        {
            throw new ConflictException("A project with this slug already exists in this company.");
        }

        var technologies = await ResolveUserTechnologiesAsync(request.TechnologyIds, userId, cancellationToken);

        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description;
        var project = Project.Create(
            companyId: request.CompanyId,
            name: request.Name,
            slug: normalizedSlug,
            description: description,
            status: request.Status,
            startDate: request.StartDate,
            endDate: request.EndDate);

        foreach (var technology in technologies)
        {
            project.ProjectTechnologies.Add(ProjectTechnology.Create(project.Id, technology.Id));
        }

        await _projectRepository.AddAsync(project, cancellationToken);
        await _projectRepository.SaveChangesAsync(cancellationToken);

        return MapToResponse(project, technologies);
    }

    public async Task<IEnumerable<ProjectResponse>> GetAllAsync(Guid? companyId = null, CancellationToken cancellationToken = default)
    {
        var userId = GetAuthenticatedUserId();
        var projects = await _projectRepository.GetAllByUserIdAsync(userId, companyId, cancellationToken);

        return projects.Select(p => MapToResponse(p));
    }

    public async Task<ProjectResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetAuthenticatedUserId();
        var project = await _projectRepository.GetByIdAndUserIdAsync(id, userId, trackChanges: false, cancellationToken);
        if (project is null)
        {
            throw new NotFoundException(nameof(Project), id);
        }

        return MapToResponse(project);
    }

    public async Task<ProjectResponse> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var userId = GetAuthenticatedUserId();
        var project = await _projectRepository.GetByIdAndUserIdAsync(id, userId, trackChanges: true, cancellationToken);
        if (project is null)
        {
            throw new NotFoundException(nameof(Project), id);
        }

        if (project.CompanyId != request.CompanyId)
        {
            var company = await _companyRepository.GetByIdAndUserIdAsync(request.CompanyId, userId, trackChanges: false, cancellationToken);
            if (company is null)
            {
                throw new NotFoundException(nameof(Company), request.CompanyId);
            }

            project.ChangeCompany(request.CompanyId);
        }

        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        var slugExists = await _projectRepository.ExistsBySlugAsync(request.CompanyId, normalizedSlug, excludeProjectId: id, cancellationToken);
        if (slugExists)
        {
            throw new ConflictException("A project with this slug already exists in this company.");
        }

        var technologies = await ResolveUserTechnologiesAsync(request.TechnologyIds, userId, cancellationToken);

        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description;
        project.UpdateDetails(
            name: request.Name,
            slug: normalizedSlug,
            description: description,
            startDate: request.StartDate,
            endDate: request.EndDate);
        project.UpdateStatus(request.Status);

        if (request.IsActive)
        {
            project.Activate();
        }
        else
        {
            project.Deactivate();
        }

        SyncProjectTechnologies(project, technologies);

        await _projectRepository.SaveChangesAsync(cancellationToken);

        return MapToResponse(project, technologies);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetAuthenticatedUserId();
        var project = await _projectRepository.GetByIdAndUserIdAsync(id, userId, trackChanges: true, cancellationToken);
        if (project is null)
        {
            throw new NotFoundException(nameof(Project), id);
        }

        await _projectRepository.DeleteAsync(project, cancellationToken);
        await _projectRepository.SaveChangesAsync(cancellationToken);
    }

    private Guid GetAuthenticatedUserId()
    {
        return _currentUserService.UserId
               ?? throw new UnauthorizedException("User is not authenticated.");
    }

    private async Task<IReadOnlyList<Technology>> ResolveUserTechnologiesAsync(
        IEnumerable<Guid>? technologyIds,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var distinctIds = technologyIds?
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (distinctIds is null || distinctIds.Count == 0)
        {
            return [];
        }

        var technologies = await _technologyRepository.GetByIdsAndUserIdAsync(distinctIds, userId, cancellationToken);
        if (technologies.Count != distinctIds.Count)
        {
            var foundIds = technologies.Select(t => t.Id).ToHashSet();
            var missingId = distinctIds.First(id => !foundIds.Contains(id));
            throw new NotFoundException(nameof(Technology), missingId);
        }

        return technologies;
    }

    private static void SyncProjectTechnologies(Project project, IReadOnlyList<Technology> desiredTechnologies)
    {
        var desiredIds = desiredTechnologies.Select(t => t.Id).ToHashSet();

        var toRemove = project.ProjectTechnologies
            .Where(pt => !desiredIds.Contains(pt.TechnologyId))
            .ToList();

        foreach (var item in toRemove)
        {
            project.ProjectTechnologies.Remove(item);
        }

        var existingIds = project.ProjectTechnologies
            .Select(pt => pt.TechnologyId)
            .ToHashSet();

        foreach (var technology in desiredTechnologies.Where(t => !existingIds.Contains(t.Id)))
        {
            project.ProjectTechnologies.Add(ProjectTechnology.Create(project.Id, technology.Id));
        }
    }

    private ProjectResponse MapToResponse(Project project, IReadOnlyList<Technology>? resolvedTechnologies = null)
    {
        var techList = resolvedTechnologies is not null
            ? resolvedTechnologies.Select(MapTechnologyResponse).ToList()
            : project.ProjectTechnologies
                .Where(pt => pt.Technology is not null)
                .Select(pt => MapTechnologyResponse(pt.Technology))
                .OrderBy(t => t.Name)
                .ToList();

        return new ProjectResponse(
            project.Id,
            project.CompanyId,
            project.Name,
            project.Slug,
            project.Description,
            project.Status.ToString(),
            project.StartDate,
            project.EndDate,
            project.IsActive,
            techList);
    }

    private TechnologyResponse MapTechnologyResponse(Technology technology)
    {
        return new TechnologyResponse(
            technology.Id,
            technology.Name,
            technology.Slug,
            technology.Description,
            _appOptions.BuildFileUrl(technology.Logo));
    }
}

