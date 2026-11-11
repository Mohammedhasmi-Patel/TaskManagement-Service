using Microsoft.Extensions.Options;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Common.Options;
using TaskManagement.Application.DTOs.Projects;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Application.Validators.Projects;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using Xunit;

namespace TaskManagement.UnitTests.Projects;

public class ProjectsServiceTests
{
    private readonly InMemoryCompanyRepository _companyRepository = new();
    private readonly InMemoryTechnologyRepository _technologyRepository = new();
    private readonly InMemoryProjectRepository _projectRepository;
    private readonly FakeCurrentUserService _currentUserService = new();
    private readonly ProjectsService _service;

    public ProjectsServiceTests()
    {
        _projectRepository = new InMemoryProjectRepository(_companyRepository);
        _service = new ProjectsService(
            _projectRepository,
            _companyRepository,
            _technologyRepository,
            _currentUserService,
            new CreateProjectRequestValidator(),
            new UpdateProjectRequestValidator(),
            Options.Create(new AppOptions { BackendUrl = "https://localhost:59131/" }));
    }

    [Fact]
    public async Task CreateAsync_WithValidRequestAndTechnologies_ShouldCreateProject()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var company = Company.Create(userId, "Acme", "acme");
        var tech = Technology.Create(userId, ".NET", "dotnet", logo: "/uploads/technologies/dotnet.png");
        await _companyRepository.AddAsync(company);
        await _technologyRepository.AddAsync(tech);

        var request = new CreateProjectRequest
        {
            CompanyId = company.Id,
            Name = "  Portal API  ",
            Slug = "portal-api",
            Description = "  Core backend  ",
            Status = ProjectStatus.InProgress,
            StartDate = new DateOnly(2026, 2, 1),
            EndDate = new DateOnly(2026, 4, 1),
            TechnologyIds = [tech.Id, tech.Id]
        };

        // Act
        var response = await _service.CreateAsync(request);

        // Assert
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal(company.Id, response.CompanyId);
        Assert.Equal("Portal API", response.Name);
        Assert.Equal("portal-api", response.Slug);
        Assert.Equal("Core backend", response.Description);
        Assert.Equal("InProgress", response.Status);
        Assert.True(response.IsActive);
        Assert.Single(response.Technologies);
        Assert.Equal("https://localhost:59131/uploads/technologies/dotnet.png", response.Technologies[0].Logo);
    }

    [Fact]
    public async Task CreateAsync_WhenCompanyBelongsToAnotherUser_ShouldThrowNotFoundException()
    {
        // Arrange
        var otherCompany = Company.Create(Guid.NewGuid(), "Other", "other");
        await _companyRepository.AddAsync(otherCompany);

        var request = new CreateProjectRequest
        {
            CompanyId = otherCompany.Id,
            Name = "Portal",
            Slug = "portal"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateSlugInSameCompany_ShouldThrowConflictException()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var company = Company.Create(userId, "Acme", "acme");
        await _companyRepository.AddAsync(company);
        await _projectRepository.AddAsync(Project.Create(company.Id, "Existing", "portal"));

        var request = new CreateProjectRequest
        {
            CompanyId = company.Id,
            Name = "Portal",
            Slug = "portal"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WithTechnologyOfAnotherUser_ShouldThrowNotFoundException()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var company = Company.Create(userId, "Acme", "acme");
        var otherTech = Technology.Create(Guid.NewGuid(), "Secret", "secret");
        await _companyRepository.AddAsync(company);
        await _technologyRepository.AddAsync(otherTech);

        var request = new CreateProjectRequest
        {
            CompanyId = company.Id,
            Name = "Portal",
            Slug = "portal",
            TechnologyIds = [otherTech.Id]
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task GetAllAsync_WithOptionalCompanyFilter_ShouldReturnOnlyUsersProjects()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var company1 = Company.Create(userId, "Co 1", "co-1");
        var company2 = Company.Create(userId, "Co 2", "co-2");
        var otherCompany = Company.Create(Guid.NewGuid(), "Other Co", "other-co");
        await _companyRepository.AddAsync(company1);
        await _companyRepository.AddAsync(company2);
        await _companyRepository.AddAsync(otherCompany);

        await _projectRepository.AddAsync(Project.Create(company1.Id, "Zeta", "zeta"));
        await _projectRepository.AddAsync(Project.Create(company1.Id, "Alpha", "alpha"));
        await _projectRepository.AddAsync(Project.Create(company2.Id, "Beta", "beta"));
        await _projectRepository.AddAsync(Project.Create(otherCompany.Id, "Hidden", "hidden"));

        // Act
        var allForUser = (await _service.GetAllAsync()).ToList();
        var filteredByCompany1 = (await _service.GetAllAsync(company1.Id)).ToList();

        // Assert
        Assert.Equal(3, allForUser.Count);
        Assert.Equal(["Alpha", "Beta", "Zeta"], allForUser.Select(p => p.Name));

        Assert.Equal(2, filteredByCompany1.Count);
        Assert.Equal(["Alpha", "Zeta"], filteredByCompany1.Select(p => p.Name));
    }

    [Fact]
    public async Task GetByIdAsync_WhenProjectBelongsToAnotherUser_ShouldThrowNotFoundException()
    {
        // Arrange
        var otherCompany = Company.Create(Guid.NewGuid(), "Other Co", "other-co");
        await _companyRepository.AddAsync(otherCompany);
        var project = Project.Create(otherCompany.Id, "Secret", "secret");
        await _projectRepository.AddAsync(project);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(project.Id));
    }

    [Fact]
    public async Task UpdateAsync_WithValidDataAndUpdatedTechnologies_ShouldUpdateProject()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var company = Company.Create(userId, "Acme", "acme");
        var tech1 = Technology.Create(userId, "C#", "csharp");
        var tech2 = Technology.Create(userId, "PostgreSQL", "postgresql");
        await _companyRepository.AddAsync(company);
        await _technologyRepository.AddAsync(tech1);
        await _technologyRepository.AddAsync(tech2);

        var project = Project.Create(company.Id, "Old", "old-slug");
        project.ProjectTechnologies.Add(ProjectTechnology.Create(project.Id, tech1.Id));
        await _projectRepository.AddAsync(project);

        var request = new UpdateProjectRequest
        {
            CompanyId = company.Id,
            Name = "New Name",
            Slug = "new-slug",
            Description = "Updated",
            Status = ProjectStatus.Completed,
            IsActive = false,
            TechnologyIds = [tech2.Id]
        };

        // Act
        var response = await _service.UpdateAsync(project.Id, request);

        // Assert
        Assert.Equal("New Name", response.Name);
        Assert.Equal("new-slug", response.Slug);
        Assert.Equal("Completed", response.Status);
        Assert.False(response.IsActive);
        Assert.Single(response.Technologies);
        Assert.Equal(tech2.Id, response.Technologies[0].Id);
    }

    [Fact]
    public async Task DeleteAsync_WhenProjectExistsForUser_ShouldRemoveProject()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var company = Company.Create(userId, "Acme", "acme");
        await _companyRepository.AddAsync(company);
        var project = Project.Create(company.Id, "To Delete", "to-delete");
        await _projectRepository.AddAsync(project);

        // Act
        await _service.DeleteAsync(project.Id);

        // Assert
        Assert.Null(await _projectRepository.GetByIdAsync(project.Id));
    }

    private sealed class InMemoryProjectRepository(
        InMemoryCompanyRepository companyRepository) : IProjectRepository
    {
        private readonly List<Project> _projects = [];

        public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_projects.FirstOrDefault(p => p.Id == id));

        public Task<Project?> GetByIdAndUserIdAsync(Guid id, Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default)
        {
            var project = _projects.FirstOrDefault(p =>
                p.Id == id && companyRepository.OwnsCompany(p.CompanyId, userId));

            return Task.FromResult(project);
        }

        public Task<IReadOnlyList<Project>> GetAllByUserIdAsync(Guid userId, Guid? companyId = null, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Project> result = _projects
                .Where(p => companyRepository.OwnsCompany(p.CompanyId, userId)
                            && (!companyId.HasValue || p.CompanyId == companyId.Value))
                .OrderBy(p => p.Name)
                .ToList();

            return Task.FromResult(result);
        }

        public Task<bool> ExistsBySlugAsync(Guid companyId, string slug, Guid? excludeProjectId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(_projects.Any(p =>
                p.CompanyId == companyId
                && p.Slug == slug
                && (!excludeProjectId.HasValue || p.Id != excludeProjectId.Value)));

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
            Task.FromResult<IReadOnlyList<Company>>(_companies.Where(c => c.UserId == userId).ToList());

        public Task<bool> ExistsBySlugAsync(Guid userId, string slug, Guid? excludeCompanyId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(_companies.Any(c => c.UserId == userId && c.Slug == slug && (!excludeCompanyId.HasValue || c.Id != excludeCompanyId.Value)));

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
            Task.FromResult<IReadOnlyList<Technology>>(_technologies.Where(t => t.UserId == userId).ToList());

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
            Task.FromResult(_technologies.Any(t => t.UserId == userId && t.Slug == slug && (!excludeTechnologyId.HasValue || t.Id != excludeTechnologyId.Value)));

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
