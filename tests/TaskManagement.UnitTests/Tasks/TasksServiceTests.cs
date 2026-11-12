using Microsoft.Extensions.Options;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Common.Options;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Application.Validators.Tasks;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using Xunit;

namespace TaskManagement.UnitTests.Tasks;

public class TasksServiceTests
{
    private readonly InMemoryCompanyRepository _companyRepository = new();
    private readonly InMemoryProjectRepository _projectRepository;
    private readonly InMemoryTechnologyRepository _technologyRepository = new();
    private readonly InMemoryTaskRepository _taskRepository;
    private readonly FakeCurrentUserService _currentUserService = new();
    private readonly TasksService _service;

    public TasksServiceTests()
    {
        _projectRepository = new InMemoryProjectRepository(_companyRepository);
        _taskRepository = new InMemoryTaskRepository(_projectRepository);
        _service = new TasksService(
            _taskRepository,
            _projectRepository,
            _technologyRepository,
            _currentUserService,
            new CreateTaskRequestValidator(),
            new UpdateTaskRequestValidator(),
            Options.Create(new AppOptions { BackendUrl = "https://localhost:59131/" }));
    }

    [Fact]
    public async Task CreateAsync_WithValidRequestAndTechnologies_ShouldCreateTask()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var company = Company.Create(userId, "Acme", "acme");
        var project = Project.Create(company.Id, "Portal", "portal");
        var tech = Technology.Create(userId, "C#", "csharp", logo: "/uploads/technologies/cs.png");
        await _companyRepository.AddAsync(company);
        await _projectRepository.AddAsync(project);
        await _technologyRepository.AddAsync(tech);

        var request = new CreateTaskRequest
        {
            ProjectId = project.Id,
            Title = "  Build API  ",
            Description = "  Create endpoints  ",
            Status = TaskStatusEnum.InProgress,
            Priority = TaskPriority.High,
            EstimatedHours = 6.5m,
            TechnologyIds = [tech.Id, tech.Id]
        };

        // Act
        var response = await _service.CreateAsync(request);

        // Assert
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal(project.Id, response.ProjectId);
        Assert.Equal("Build API", response.Title);
        Assert.Equal("Create endpoints", response.Description);
        Assert.Equal("InProgress", response.Status);
        Assert.Equal("High", response.Priority);
        Assert.Equal(6.5m, response.EstimatedHours);
        Assert.Single(response.Technologies);
        Assert.Equal("https://localhost:59131/uploads/technologies/cs.png", response.Technologies[0].Logo);
    }

    [Fact]
    public async Task CreateAsync_WhenProjectBelongsToAnotherUser_ShouldThrowNotFoundException()
    {
        // Arrange
        var otherCompany = Company.Create(Guid.NewGuid(), "Other", "other");
        var otherProject = Project.Create(otherCompany.Id, "Secret", "secret");
        await _companyRepository.AddAsync(otherCompany);
        await _projectRepository.AddAsync(otherProject);

        var request = new CreateTaskRequest
        {
            ProjectId = otherProject.Id,
            Title = "Unauthorized Task"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WithEmptyGuidInTechnologyIds_ShouldThrowNotFoundException()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var company = Company.Create(userId, "Acme", "acme");
        var project = Project.Create(company.Id, "Portal", "portal");
        await _companyRepository.AddAsync(company);
        await _projectRepository.AddAsync(project);

        var request = new CreateTaskRequest
        {
            ProjectId = project.Id,
            Title = "Task",
            TechnologyIds = [Guid.Empty]
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task GetAllAsync_WithFilters_ShouldReturnFilteredTasksForCurrentUser()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var company = Company.Create(userId, "Acme", "acme");
        var project1 = Project.Create(company.Id, "P1", "p1");
        var project2 = Project.Create(company.Id, "P2", "p2");
        var otherCompany = Company.Create(Guid.NewGuid(), "Other", "other");
        var otherProject = Project.Create(otherCompany.Id, "Other P", "other-p");

        await _companyRepository.AddAsync(company);
        await _companyRepository.AddAsync(otherCompany);
        await _projectRepository.AddAsync(project1);
        await _projectRepository.AddAsync(project2);
        await _projectRepository.AddAsync(otherProject);

        await _taskRepository.AddAsync(TaskItem.Create(project1.Id, "Zeta Task", status: TaskStatusEnum.Todo, priority: TaskPriority.Low));
        await _taskRepository.AddAsync(TaskItem.Create(project1.Id, "Alpha Task", status: TaskStatusEnum.InProgress, priority: TaskPriority.High));
        await _taskRepository.AddAsync(TaskItem.Create(project2.Id, "Beta Task", status: TaskStatusEnum.InProgress, priority: TaskPriority.High));
        await _taskRepository.AddAsync(TaskItem.Create(otherProject.Id, "Hidden Task", status: TaskStatusEnum.InProgress, priority: TaskPriority.High));

        // Act
        var all = (await _service.GetAllAsync()).ToList();
        var filtered = (await _service.GetAllAsync(project1.Id, TaskStatusEnum.InProgress, TaskPriority.High)).ToList();

        // Assert
        Assert.Equal(3, all.Count);
        Assert.Equal(["Alpha Task", "Beta Task", "Zeta Task"], all.Select(t => t.Title));

        Assert.Single(filtered);
        Assert.Equal("Alpha Task", filtered[0].Title);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTaskBelongsToAnotherUser_ShouldThrowNotFoundException()
    {
        // Arrange
        var otherCompany = Company.Create(Guid.NewGuid(), "Other", "other");
        var otherProject = Project.Create(otherCompany.Id, "Secret", "secret");
        var otherTask = TaskItem.Create(otherProject.Id, "Secret Task");
        await _companyRepository.AddAsync(otherCompany);
        await _projectRepository.AddAsync(otherProject);
        await _taskRepository.AddAsync(otherTask);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(otherTask.Id));
    }

    [Fact]
    public async Task UpdateAsync_WithValidDataAndTechnologies_ShouldUpdateTask()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var company = Company.Create(userId, "Acme", "acme");
        var project1 = Project.Create(company.Id, "P1", "p1");
        var project2 = Project.Create(company.Id, "P2", "p2");
        var tech1 = Technology.Create(userId, "C#", "csharp");
        var tech2 = Technology.Create(userId, "Docker", "docker");
        await _companyRepository.AddAsync(company);
        await _projectRepository.AddAsync(project1);
        await _projectRepository.AddAsync(project2);
        await _technologyRepository.AddAsync(tech1);
        await _technologyRepository.AddAsync(tech2);

        var task = TaskItem.Create(project1.Id, "Old Title");
        task.TaskTechnologies.Add(TaskTechnology.Create(task.Id, tech1.Id));
        await _taskRepository.AddAsync(task);

        var request = new UpdateTaskRequest
        {
            ProjectId = project2.Id,
            Title = "New Title",
            Description = "Updated description",
            Status = TaskStatusEnum.Completed,
            Priority = TaskPriority.Critical,
            EstimatedHours = 8m,
            TechnologyIds = [tech2.Id]
        };

        // Act
        var response = await _service.UpdateAsync(task.Id, request);

        // Assert
        Assert.Equal(project2.Id, response.ProjectId);
        Assert.Equal("New Title", response.Title);
        Assert.Equal("Updated description", response.Description);
        Assert.Equal("Completed", response.Status);
        Assert.Equal("Critical", response.Priority);
        Assert.Equal(8m, response.EstimatedHours);
        Assert.Single(response.Technologies);
        Assert.Equal(tech2.Id, response.Technologies[0].Id);
    }

    [Fact]
    public async Task DeleteAsync_WhenTaskExistsForUser_ShouldRemoveTask()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var company = Company.Create(userId, "Acme", "acme");
        var project = Project.Create(company.Id, "P1", "p1");
        var task = TaskItem.Create(project.Id, "To Delete");
        await _companyRepository.AddAsync(company);
        await _projectRepository.AddAsync(project);
        await _taskRepository.AddAsync(task);

        // Act
        await _service.DeleteAsync(task.Id);

        // Assert
        Assert.Null(await _taskRepository.GetByIdAsync(task.Id));
    }

    private sealed class InMemoryTaskRepository(InMemoryProjectRepository projectRepository) : ITaskRepository
    {
        private readonly List<TaskItem> _tasks = [];

        public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_tasks.FirstOrDefault(t => t.Id == id));

        public Task<TaskItem?> GetByIdAndUserIdAsync(Guid id, Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default)
        {
            var task = _tasks.FirstOrDefault(t =>
                t.Id == id && projectRepository.OwnsProject(t.ProjectId, userId));

            return Task.FromResult(task);
        }

        public Task<IReadOnlyList<TaskItem>> GetAllByUserIdAsync(
            Guid userId,
            Guid? projectId = null,
            TaskStatusEnum? status = null,
            TaskPriority? priority = null,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<TaskItem> result = _tasks
                .Where(t =>
                    projectRepository.OwnsProject(t.ProjectId, userId)
                    && (!projectId.HasValue || t.ProjectId == projectId.Value)
                    && (!status.HasValue || t.Status == status.Value)
                    && (!priority.HasValue || t.Priority == priority.Value))
                .OrderBy(t => t.Title)
                .ToList();

            return Task.FromResult(result);
        }

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
            Task.FromResult<IReadOnlyList<Project>>(_projects.Where(p => companyRepository.OwnsCompany(p.CompanyId, userId)).ToList());

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

    private sealed class InMemoryTechnologyRepository : ITechnologyRepository
    {
        private readonly List<Technology> _technologies = [];

        public Task<Technology?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_technologies.FirstOrDefault(t => t.Id == id));

        public Task<Technology?> GetByIdAndUserIdAsync(Guid id, Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(_technologies.FirstOrDefault(t => t.Id == id && t.UserId == userId));

        public Task<IReadOnlyList<Technology>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Technology>>([]);

        public Task<IReadOnlyList<Technology>> GetByIdsAndUserIdAsync(IEnumerable<Guid> ids, Guid userId, CancellationToken cancellationToken = default)
        {
            var idSet = ids.ToHashSet();
            IReadOnlyList<Technology> result = _technologies
                .Where(t => t.UserId == userId && idSet.Contains(t.Id))
                .OrderBy(t => t.Name)
                .ToList();

            return Task.FromResult(result);
        }

        public Task<bool> ExistsBySlugAsync(Guid userId, string slug, Guid? excludeTechnologyId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task AddAsync(Technology technology, CancellationToken cancellationToken = default)
        {
            _technologies.Add(technology);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Technology technology, CancellationToken cancellationToken = default)
        {
            _technologies.Remove(technology);
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

