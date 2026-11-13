using FluentValidation;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.DTOs.TimeEntries;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using ValidationException = TaskManagement.Application.Common.Exceptions.ValidationException;

namespace TaskManagement.Application.Services;

public class TimeEntriesService(
    ITaskTimeEntryRepository timeEntryRepository,
    ITaskRepository taskRepository,
    ICurrentUserService currentUserService,
    IValidator<CreateTimeEntryRequest> createValidator,
    IValidator<UpdateTimeEntryRequest> updateValidator) : ITimeEntriesService
{
    private readonly ITaskTimeEntryRepository _timeEntryRepository = timeEntryRepository;
    private readonly ITaskRepository _taskRepository = taskRepository;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IValidator<CreateTimeEntryRequest> _createValidator = createValidator;
    private readonly IValidator<UpdateTimeEntryRequest> _updateValidator = updateValidator;

    public async Task<TimeEntryResponse> CreateAsync(CreateTimeEntryRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var userId = GetAuthenticatedUserId();
        var task = await _taskRepository.GetByIdAndUserIdAsync(request.TaskId, userId, trackChanges: true, cancellationToken);
        if (task is null)
        {
            throw new NotFoundException(nameof(TaskItem), request.TaskId);
        }

        if (task.Status == TaskStatusEnum.Todo || (request.EndTime is null && task.Status != TaskStatusEnum.InProgress))
        {
            task.UpdateStatus(TaskStatusEnum.InProgress);
            await _taskRepository.SaveChangesAsync(cancellationToken);
        }

        var startTime = (request.StartTime ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var endTime = request.EndTime?.ToUniversalTime();
        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description;

        var entry = TaskTimeEntry.Create(
            taskId: request.TaskId,
            startTime: startTime,
            endTime: endTime,
            description: description,
            durationSeconds: request.DurationSeconds);

        await _timeEntryRepository.AddAsync(entry, cancellationToken);
        await _timeEntryRepository.SaveChangesAsync(cancellationToken);

        return MapToResponse(entry);
    }

    public async Task<IEnumerable<TimeEntryResponse>> GetAllAsync(Guid? taskId = null, CancellationToken cancellationToken = default)
    {
        var userId = GetAuthenticatedUserId();
        var entries = await _timeEntryRepository.GetAllByUserIdAsync(userId, taskId, cancellationToken);

        return entries.Select(MapToResponse);
    }

    public async Task<TimeEntryResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetAuthenticatedUserId();
        var entry = await _timeEntryRepository.GetByIdAndUserIdAsync(id, userId, trackChanges: false, cancellationToken);
        if (entry is null)
        {
            throw new NotFoundException(nameof(TaskTimeEntry), id);
        }

        return MapToResponse(entry);
    }

    public async Task<TimeEntryResponse> UpdateAsync(Guid id, UpdateTimeEntryRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var userId = GetAuthenticatedUserId();
        var entry = await _timeEntryRepository.GetByIdAndUserIdAsync(id, userId, trackChanges: true, cancellationToken);
        if (entry is null)
        {
            throw new NotFoundException(nameof(TaskTimeEntry), id);
        }

        if (entry.TaskId != request.TaskId)
        {
            throw new BadRequestException("Task ID cannot be changed for an existing time entry.");
        }

        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description;
        entry.UpdateDetails(
            startTime: request.StartTime.ToUniversalTime(),
            endTime: request.EndTime?.ToUniversalTime(),
            description: description,
            durationSeconds: request.DurationSeconds);

        await _timeEntryRepository.SaveChangesAsync(cancellationToken);
        return MapToResponse(entry);
    }

    public async Task<TimeEntryResponse> StopAsync(Guid id, StopTimeEntryRequest? request = null, CancellationToken cancellationToken = default)
    {
        var userId = GetAuthenticatedUserId();
        var entry = await _timeEntryRepository.GetByIdAndUserIdAsync(id, userId, trackChanges: true, cancellationToken);
        if (entry is null)
        {
            throw new NotFoundException(nameof(TaskTimeEntry), id);
        }

        if (entry.EndTime.HasValue)
        {
            throw new BadRequestException("Time entry is already stopped.");
        }

        if (request?.Description?.Length > 1000)
        {
            throw new BadRequestException("Description must not exceed 1000 characters.");
        }

        var nowUtc = DateTimeOffset.UtcNow;
        if (request?.EndTime is { } requestedEndTime)
        {
            var explicitEndTime = requestedEndTime.ToUniversalTime();
            if (explicitEndTime < entry.StartTime)
            {
                throw new BadRequestException("End time must be on or after start time.");
            }

            entry.Stop(explicitEndTime, request.Description);
        }
        else
        {
            var effectiveEndTime = nowUtc < entry.StartTime ? entry.StartTime : nowUtc;
            entry.Stop(effectiveEndTime, request?.Description);
        }

        await _timeEntryRepository.SaveChangesAsync(cancellationToken);

        return MapToResponse(entry);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetAuthenticatedUserId();
        var entry = await _timeEntryRepository.GetByIdAndUserIdAsync(id, userId, trackChanges: true, cancellationToken);
        if (entry is null)
        {
            throw new NotFoundException(nameof(TaskTimeEntry), id);
        }

        await _timeEntryRepository.DeleteAsync(entry, cancellationToken);
        await _timeEntryRepository.SaveChangesAsync(cancellationToken);
    }

    private Guid GetAuthenticatedUserId()
    {
        return _currentUserService.UserId
               ?? throw new UnauthorizedException("User is not authenticated.");
    }

    private static TimeEntryResponse MapToResponse(TaskTimeEntry entry)
    {
        return new TimeEntryResponse(
            entry.Id,
            entry.TaskId,
            entry.StartTime,
            entry.EndTime,
            entry.DurationSeconds,
            entry.Description,
            !entry.EndTime.HasValue,
            entry.CreatedAt,
            entry.UpdatedAt);
    }
}

