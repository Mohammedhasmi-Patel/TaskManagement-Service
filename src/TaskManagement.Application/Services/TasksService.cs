using FluentValidation;
using Microsoft.Extensions.Options;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Common.Options;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Application.DTOs.Technologies;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using ValidationException = TaskManagement.Application.Common.Exceptions.ValidationException;

namespace TaskManagement.Application.Services;

public class TasksService(
    ITaskRepository taskRepository,
    IProjectRepository projectRepository,
    ITechnologyRepository technologyRepository,
    ICurrentUserService currentUserService,
    IValidator<CreateTaskRequest> createValidator,
    IValidator<UpdateTaskRequest> updateValidator,
    IOptions<AppOptions> appOptions) : ITasksService
{
    private readonly ITaskRepository _taskRepository = taskRepository;
    private readonly IProjectRepository _projectRepository = projectRepository;
    private readonly ITechnologyRepository _technologyRepository = technologyRepository;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IValidator<CreateTaskRequest> _createValidator = createValidator;
    private readonly IValidator<UpdateTaskRequest> _updateValidator = updateValidator;
    private readonly AppOptions _appOptions = appOptions.Value;

    public async Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var userId = GetAuthenticatedUserId();

        var project = await _projectRepository.GetByIdAndUserIdAsync(request.ProjectId, userId, trackChanges: false, cancellationToken);
        if (project is null)
        {
            throw new NotFoundException(nameof(Project), request.ProjectId);
        }

        var technologies = await ResolveUserTechnologiesAsync(request.TechnologyIds, userId, cancellationToken);

        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description;
        var task = TaskItem.Create(
            projectId: request.ProjectId,
            title: request.Title,
            description: description,
            status: request.Status,
            priority: request.Priority,
            estimatedHours: request.EstimatedHours);

        foreach (var technology in technologies)
        {
            task.TaskTechnologies.Add(TaskTechnology.Create(task.Id, technology.Id));
        }

        await _taskRepository.AddAsync(task, cancellationToken);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        return MapToResponse(task, technologies);
    }

    public async Task<IEnumerable<TaskResponse>> GetAllAsync(
        Guid? projectId = null,
        TaskStatusEnum? status = null,
        TaskPriority? priority = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetAuthenticatedUserId();
        var tasks = await _taskRepository.GetAllByUserIdAsync(
            userId,
            projectId,
            status,
            priority,
            fromDate,
            toDate,
            cancellationToken);

        return tasks.Select(t => MapToResponse(t));
    }

    public async Task<TaskResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetAuthenticatedUserId();
        var task = await _taskRepository.GetByIdAndUserIdAsync(id, userId, trackChanges: false, cancellationToken);
        
        if (task is null)
        {
            throw new NotFoundException(nameof(TaskItem), id);
        }

        return MapToResponse(task);
    }

    public async Task<TaskResponse> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var userId = GetAuthenticatedUserId();
        var task = await _taskRepository.GetByIdAndUserIdAsync(id, userId, trackChanges: true, cancellationToken);
        if (task is null)
        {
            throw new NotFoundException(nameof(TaskItem), id);
        }

        if (task.ProjectId != request.ProjectId)
        {
            var project = await _projectRepository.GetByIdAndUserIdAsync(request.ProjectId, userId, trackChanges: false, cancellationToken);
            if (project is null)
            {
                throw new NotFoundException(nameof(Project), request.ProjectId);
            }

            task.ChangeProject(request.ProjectId);
        }

        var technologies = await ResolveUserTechnologiesAsync(request.TechnologyIds, userId, cancellationToken);

        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description;
        task.UpdateDetails(
            title: request.Title,
            description: description,
            estimatedHours: request.EstimatedHours);
        task.UpdateStatus(request.Status);
        task.UpdatePriority(request.Priority);

        SyncTaskTechnologies(task, technologies);

        await _taskRepository.SaveChangesAsync(cancellationToken);

        return MapToResponse(task, technologies);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetAuthenticatedUserId();
        var task = await _taskRepository.GetByIdAndUserIdAsync(id, userId, trackChanges: true, cancellationToken);
        if (task is null)
        {
            throw new NotFoundException(nameof(TaskItem), id);
        }

        await _taskRepository.DeleteAsync(task, cancellationToken);
        await _taskRepository.SaveChangesAsync(cancellationToken);
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

    private static void SyncTaskTechnologies(TaskItem task, IReadOnlyList<Technology> desiredTechnologies)
    {
        var desiredIds = desiredTechnologies.Select(t => t.Id).ToHashSet();

        var toRemove = task.TaskTechnologies
            .Where(tt => !desiredIds.Contains(tt.TechnologyId))
            .ToList();

        foreach (var item in toRemove)
        {
            task.TaskTechnologies.Remove(item);
        }

        var existingIds = task.TaskTechnologies
            .Select(tt => tt.TechnologyId)
            .ToHashSet();

        foreach (var technology in desiredTechnologies.Where(t => !existingIds.Contains(t.Id)))
        {
            task.TaskTechnologies.Add(TaskTechnology.Create(task.Id, technology.Id));
        }
    }

    private TaskResponse MapToResponse(TaskItem task, IReadOnlyList<Technology>? resolvedTechnologies = null)
    {
        var techList = resolvedTechnologies is not null
            ? resolvedTechnologies.Select(MapTechnologyResponse).ToList()
            : task.TaskTechnologies
                .Where(tt => tt.Technology is not null)
                .Select(tt => MapTechnologyResponse(tt.Technology))
                .OrderBy(t => t.Name)
                .ToList();

        var timeEntries = task.TimeEntries
            .OrderByDescending(te => te.StartTime)
            .Select(MapTaskTimeEntryResponse)
            .ToList();

        return new TaskResponse(
            task.Id,
            task.ProjectId,
            task.Title,
            task.Description,
            task.Status.ToString(),
            task.Priority.ToString(),
            task.EstimatedHours,
            task.CreatedAt,
            task.UpdatedAt,
            techList,
            timeEntries);
    }

    private static TaskTimeEntryResponse MapTaskTimeEntryResponse(TaskTimeEntry entry)
    {
        var totalTime = entry.DurationSeconds > 0
            ? entry.DurationSeconds
            : (entry.EndTime.HasValue && entry.EndTime.Value > entry.StartTime
                ? (long)(entry.EndTime.Value - entry.StartTime).TotalSeconds
                : 0);

        return new TaskTimeEntryResponse(
            entry.Description,
            entry.StartTime,
            entry.EndTime,
            totalTime);
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

