using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.DTOs.TimeEntries;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Application.Validators.TimeEntries;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using Xunit;

namespace TaskManagement.UnitTests.TimeEntries;

public class TimeEntriesServiceTests
{
    private readonly InMemoryCompanyRepository _companyRepository = new();
    private readonly InMemoryProjectRepository _projectRepository;
    private readonly InMemoryTaskRepository _taskRepository;
    private readonly InMemoryTaskTimeEntryRepository _timeEntryRepository;
    private readonly FakeCurrentUserService _currentUserService = new();
    private readonly TimeEntriesService _service;

    public TimeEntriesServiceTests()
    {
        _projectRepository = new InMemoryProjectRepository(_companyRepository);
        _taskRepository = new InMemoryTaskRepository(_projectRepository);
        _timeEntryRepository = new InMemoryTaskTimeEntryRepository(_taskRepository);
        _service = new TimeEntriesService(
            _timeEntryRepository,
            _taskRepository,
            _currentUserService,
            new CreateTimeEntryRequestValidator(),
            new UpdateTimeEntryRequestValidator());
    }

    [Fact]
    public async Task CreateAsync_WithoutEndTime_ShouldCreateRunningTimerInUtc()
    {
        // Arrange
        var task = await SeedOwnedTaskAsync();
        var localStart = new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.FromHours(5.5));
        var request = new CreateTimeEntryRequest
        {
            TaskId = task.Id,
            StartTime = localStart,
            Description = "  Working on API  "
        };

        // Act
        var response = await _service.CreateAsync(request);

        // Assert
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal(task.Id, response.TaskId);
        Assert.Equal(TimeSpan.Zero, response.StartTime.Offset);
        Assert.Equal(localStart.ToUniversalTime(), response.StartTime);
        Assert.Null(response.EndTime);
        Assert.Equal(0, response.DurationSeconds);
        Assert.True(response.IsRunning);
        Assert.Equal("Working on API", response.Description);
        Assert.Equal(TaskStatusEnum.InProgress, task.Status);
    }

    [Fact]
    public async Task CreateAsync_WhenTaskIsTodo_ShouldTransitionTaskStatusToInProgress()
    {
        // Arrange
        var task = await SeedOwnedTaskAsync();
        Assert.Equal(TaskStatusEnum.Todo, task.Status);

        // Act
        await _service.CreateAsync(new CreateTimeEntryRequest
        {
            TaskId = task.Id
        });

        // Assert
        Assert.Equal(TaskStatusEnum.InProgress, task.Status);
    }

    [Fact]
    public async Task CreateAsync_WithStartAndEndTime_ShouldComputeDurationSeconds()
    {
        // Arrange
        var task = await SeedOwnedTaskAsync();
        var start = new DateTimeOffset(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);
        var end = start.AddMinutes(45);

        // Act
        var response = await _service.CreateAsync(new CreateTimeEntryRequest
        {
            TaskId = task.Id,
            StartTime = start,
            EndTime = end
        });

        Assert.False(response.IsRunning);
        Assert.Equal(2700, response.DurationSeconds);
    }

    [Fact]
    public async Task CreateAsync_WhenTaskBelongsToAnotherUser_ShouldThrowNotFoundException()
    {
        // Arrange
        var otherCompany = Company.Create(Guid.NewGuid(), "Other", "other");
        var otherProject = Project.Create(otherCompany.Id, "Other P", "other-p");
        var otherTask = TaskItem.Create(otherProject.Id, "Secret Task");
        await _companyRepository.AddAsync(otherCompany);
        await _projectRepository.AddAsync(otherProject);
        await _taskRepository.AddAsync(otherTask);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(new CreateTimeEntryRequest
        {
            TaskId = otherTask.Id
        }));
    }

    [Fact]
    public async Task GetAllAsync_WithTaskFilter_ShouldReturnOnlyUsersEntriesOrderedByStartTimeDesc()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var company = Company.Create(userId, "Acme", "acme");
        var project = Project.Create(company.Id, "Portal", "portal");
        var task1 = TaskItem.Create(project.Id, "Task 1");
        var task2 = TaskItem.Create(project.Id, "Task 2");
        var otherCompany = Company.Create(Guid.NewGuid(), "Other", "other");
        var otherProject = Project.Create(otherCompany.Id, "Other", "other");
        var otherTask = TaskItem.Create(otherProject.Id, "Other");

        await _companyRepository.AddAsync(company);
        await _companyRepository.AddAsync(otherCompany);
        await _projectRepository.AddAsync(project);
        await _projectRepository.AddAsync(otherProject);
        await _taskRepository.AddAsync(task1);
        await _taskRepository.AddAsync(task2);
        await _taskRepository.AddAsync(otherTask);

        var t0 = DateTimeOffset.UtcNow.AddHours(-3);
        await _timeEntryRepository.AddAsync(TaskTimeEntry.Create(task1.Id, t0));
        await _timeEntryRepository.AddAsync(TaskTimeEntry.Create(task1.Id, t0.AddHours(1)));
        await _timeEntryRepository.AddAsync(TaskTimeEntry.Create(task2.Id, t0.AddHours(2)));
        await _timeEntryRepository.AddAsync(TaskTimeEntry.Create(otherTask.Id, t0.AddHours(3)));

        // Act
        var all = (await _service.GetAllAsync()).ToList();
        var filtered = (await _service.GetAllAsync(task1.Id)).ToList();

        // Assert
        Assert.Equal(3, all.Count);
        Assert.Equal(t0.AddHours(2), all[0].StartTime);
        Assert.Equal(2, filtered.Count);
        Assert.Equal(t0.AddHours(1), filtered[0].StartTime);
    }

    [Fact]
    public async Task StopAsync_WhenTimerIsRunning_ShouldSetEndTimeAndDuration()
    {
        // Arrange
        var task = await SeedOwnedTaskAsync();
        var start = new DateTimeOffset(2026, 11, 12, 8, 0, 0, TimeSpan.Zero);
        var entry = TaskTimeEntry.Create(task.Id, start);
        await _timeEntryRepository.AddAsync(entry);

        // Act
        var response = await _service.StopAsync(entry.Id, new StopTimeEntryRequest
        {
            EndTime = start.AddHours(1.5),
            Description = "  Finished API work  "
        });

        // Assert
        Assert.False(response.IsRunning);
        Assert.Equal(5400, response.DurationSeconds);
        Assert.Equal(start.AddHours(1.5), response.EndTime);
        Assert.Equal("Finished API work", response.Description);
    }

    [Fact]
    public async Task StopAsync_WithoutExplicitEndTime_WhenStartTimeIsInFuture_ShouldClampToStartTime()
    {
        // Arrange
        var task = await SeedOwnedTaskAsync();
        var futureStart = DateTimeOffset.UtcNow.AddMinutes(30);
        var entry = TaskTimeEntry.Create(task.Id, futureStart);
        await _timeEntryRepository.AddAsync(entry);

        // Act
        var response = await _service.StopAsync(entry.Id);

        // Assert
        Assert.False(response.IsRunning);
        Assert.Equal(0, response.DurationSeconds);
        Assert.Equal(futureStart, response.EndTime);
    }

    [Fact]
    public async Task StopAsync_WithExplicitEndTimeBeforeStartTime_ShouldThrowBadRequestException()
    {
        // Arrange
        var task = await SeedOwnedTaskAsync();
        var start = new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);
        var entry = TaskTimeEntry.Create(task.Id, start);
        await _timeEntryRepository.AddAsync(entry);

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.StopAsync(entry.Id, new StopTimeEntryRequest { EndTime = start.AddMinutes(-5) }));
    }

    [Fact]
    public async Task StopAsync_WhenAlreadyStopped_ShouldThrowBadRequestException()
    {
        // Arrange
        var task = await SeedOwnedTaskAsync();
        var start = DateTimeOffset.UtcNow.AddHours(-1);
        var entry = TaskTimeEntry.Create(task.Id, start, start.AddMinutes(30));
        await _timeEntryRepository.AddAsync(entry);

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _service.StopAsync(entry.Id));
    }

    [Fact]
    public async Task StopAsync_WhenEndTimeBeforeStartTime_ShouldThrowBadRequestException()
    {
        // Arrange
        var task = await SeedOwnedTaskAsync();
        var start = DateTimeOffset.UtcNow;
        var entry = TaskTimeEntry.Create(task.Id, start);
        await _timeEntryRepository.AddAsync(entry);

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.StopAsync(entry.Id, new StopTimeEntryRequest { EndTime = start.AddMinutes(-5) }));
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_ShouldUpdateEntry()
    {
        // Arrange
        var task = await SeedOwnedTaskAsync();
        var start = new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);
        var entry = TaskTimeEntry.Create(task.Id, start);
        await _timeEntryRepository.AddAsync(entry);

        // Act
        var response = await _service.UpdateAsync(entry.Id, new UpdateTimeEntryRequest
        {
            TaskId = task.Id,
            StartTime = start,
            EndTime = start.AddMinutes(20),
            Description = "Updated note"
        });

        // Assert
        Assert.Equal(task.Id, response.TaskId);
        Assert.Equal(1200, response.DurationSeconds);
        Assert.False(response.IsRunning);
        Assert.Equal("Updated note", response.Description);
    }

    [Fact]
    public async Task UpdateAsync_WhenTaskIdDoesNotMatchEntry_ShouldThrowBadRequestException()
    {
        // Arrange
        var task = await SeedOwnedTaskAsync();
        var start = new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);
        var entry = TaskTimeEntry.Create(task.Id, start);
        await _timeEntryRepository.AddAsync(entry);

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _service.UpdateAsync(entry.Id, new UpdateTimeEntryRequest
        {
            TaskId = Guid.NewGuid(),
            StartTime = start,
            EndTime = start.AddMinutes(20)
        }));
    }

    [Fact]
    public async Task DeleteAsync_WhenEntryExistsForUser_ShouldRemoveEntry()
    {
        // Arrange
        var task = await SeedOwnedTaskAsync();
        var entry = TaskTimeEntry.Create(task.Id, DateTimeOffset.UtcNow);
        await _timeEntryRepository.AddAsync(entry);

        // Act
        await _service.DeleteAsync(entry.Id);

        // Assert
        Assert.Null(await _timeEntryRepository.GetByIdAsync(entry.Id));
    }

    private async Task<TaskItem> SeedOwnedTaskAsync()
    {
        var userId = _currentUserService.UserId!.Value;
        var company = Company.Create(userId, "Acme", "acme");
        var project = Project.Create(company.Id, "Portal", "portal");
        var task = TaskItem.Create(project.Id, "Build API");
        await _companyRepository.AddAsync(company);
        await _projectRepository.AddAsync(project);
        await _taskRepository.AddAsync(task);
        return task;
    }

    private sealed class InMemoryTaskTimeEntryRepository(InMemoryTaskRepository taskRepository) : ITaskTimeEntryRepository
    {
        private readonly List<TaskTimeEntry> _entries = [];

        public Task<TaskTimeEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_entries.FirstOrDefault(e => e.Id == id));

        public Task<TaskTimeEntry?> GetByIdAndUserIdAsync(Guid id, Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default)
        {
            var entry = _entries.FirstOrDefault(e => e.Id == id && taskRepository.OwnsTask(e.TaskId, userId));
            return Task.FromResult(entry);
        }

        public Task<IReadOnlyList<TaskTimeEntry>> GetAllByUserIdAsync(Guid userId, Guid? taskId = null, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<TaskTimeEntry> result = _entries
                .Where(e => taskRepository.OwnsTask(e.TaskId, userId) && (!taskId.HasValue || e.TaskId == taskId.Value))
                .OrderByDescending(e => e.StartTime)
                .ToList();

            return Task.FromResult(result);
        }

        public Task AddAsync(TaskTimeEntry entry, CancellationToken cancellationToken = default)
        {
            _entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(TaskTimeEntry entry, CancellationToken cancellationToken = default)
        {
            _entries.Remove(entry);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryTaskRepository(InMemoryProjectRepository projectRepository) : ITaskRepository
    {
        private readonly List<TaskItem> _tasks = [];

        public bool OwnsTask(Guid taskId, Guid userId) =>
            _tasks.Any(t => t.Id == taskId && projectRepository.OwnsProject(t.ProjectId, userId));

        public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_tasks.FirstOrDefault(t => t.Id == id));

        public Task<TaskItem?> GetByIdAndUserIdAsync(Guid id, Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(_tasks.FirstOrDefault(t => t.Id == id && projectRepository.OwnsProject(t.ProjectId, userId)));

        public Task<IReadOnlyList<TaskItem>> GetAllByUserIdAsync(Guid userId, Guid? projectId = null, TaskStatusEnum? status = null, TaskPriority? priority = null, DateTimeOffset? fromDate = null, DateTimeOffset? toDate = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TaskItem>>([]);

        public Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
        {
            _tasks.Add(task);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(TaskItem task, CancellationToken cancellationToken = default)
        {
            _tasks.Remove(task);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryProjectRepository(InMemoryCompanyRepository companyRepository) : IProjectRepository
    {
        private readonly List<Project> _projects = [];

        public bool OwnsProject(Guid projectId, Guid userId) =>
            _projects.Any(p => p.Id == projectId && companyRepository.OwnsCompany(p.CompanyId, userId));

        public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_projects.FirstOrDefault(p => p.Id == id));

        public Task<Project?> GetByIdAndUserIdAsync(Guid id, Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(_projects.FirstOrDefault(p => p.Id == id && companyRepository.OwnsCompany(p.CompanyId, userId)));

        public Task<IReadOnlyList<Project>> GetAllByUserIdAsync(Guid userId, Guid? companyId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Project>>([]);

        public Task<bool> ExistsBySlugAsync(Guid companyId, string slug, Guid? excludeProjectId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task AddAsync(Project project, CancellationToken cancellationToken = default)
        {
            _projects.Add(project);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Project project, CancellationToken cancellationToken = default)
        {
            _projects.Remove(project);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryCompanyRepository : ICompanyRepository
    {
        private readonly List<Company> _companies = [];

        public bool OwnsCompany(Guid companyId, Guid userId) =>
            _companies.Any(c => c.Id == companyId && c.UserId == userId);

        public Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_companies.FirstOrDefault(c => c.Id == id));

        public Task<Company?> GetByIdAndUserIdAsync(Guid id, Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(_companies.FirstOrDefault(c => c.Id == id && c.UserId == userId));

        public Task<IReadOnlyList<Company>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Company>>([]);

        public Task<bool> ExistsBySlugAsync(Guid userId, string slug, Guid? excludeCompanyId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task ClearDefaultForUserAsync(Guid userId, Guid? excludeCompanyId = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task AddAsync(Company company, CancellationToken cancellationToken = default)
        {
            _companies.Add(company);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Company company, CancellationToken cancellationToken = default)
        {
            _companies.Remove(company);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? UserId { get; set; } = Guid.NewGuid();
        public string? Email => "user@example.com";
        public bool IsAuthenticated => UserId.HasValue;
    }
}

