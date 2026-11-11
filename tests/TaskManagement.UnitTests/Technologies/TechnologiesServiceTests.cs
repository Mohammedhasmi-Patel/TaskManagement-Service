using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Common.Options;
using TaskManagement.Application.DTOs.Storage;
using TaskManagement.Application.DTOs.Technologies;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Application.Validators.Technologies;
using TaskManagement.Domain.Entities;
using Xunit;

namespace TaskManagement.UnitTests.Technologies;

public class TechnologiesServiceTests
{
    private readonly InMemoryTechnologyRepository _technologyRepository = new();
    private readonly FakeCurrentUserService _currentUserService = new();
    private readonly FakeStorageService _storageService = new();
    private readonly TechnologiesService _service;

    public TechnologiesServiceTests()
    {
        _service = new TechnologiesService(
            _technologyRepository,
            _currentUserService,
            _storageService,
            new CreateTechnologyRequestValidator(),
            new UpdateTechnologyRequestValidator(),
            Options.Create(new AppOptions { BackendUrl = "https://localhost:59131/" }));
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldCreateTechnology()
    {
        // Arrange
        var request = new CreateTechnologyRequest
        {
            Name = "  TypeScript  ",
            Slug = "typescript",
            Description = "  Typed JavaScript  "
        };

        // Act
        var response = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal("TypeScript", response.Name);
        Assert.Equal("typescript", response.Slug);
        Assert.Equal("Typed JavaScript", response.Description);
        Assert.Null(response.Logo);

        var saved = await _technologyRepository.GetByIdAsync(response.Id);
        Assert.NotNull(saved);
        Assert.Equal(_currentUserService.UserId, saved.UserId);
    }

    [Fact]
    public async Task CreateAsync_WithLogo_ShouldUploadLogoAndReturnFullUrl()
    {
        // Arrange
        using var stream = new MemoryStream([1, 2, 3, 4]);
        var request = new CreateTechnologyRequest
        {
            Name = "PostgreSQL",
            Slug = "postgresql",
            Logo = new FormFile(stream, 0, stream.Length, "Logo", "pg.webp")
        };

        // Act
        var response = await _service.CreateAsync(request);

        // Assert
        Assert.Equal("https://localhost:59131/uploads/technologies/uploaded-pg.webp", response.Logo);
        Assert.Single(_storageService.UploadedFolders);
        Assert.Equal("technologies", _storageService.UploadedFolders[0]);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidRequest_ShouldThrowValidationException()
    {
        // Arrange
        var request = new CreateTechnologyRequest
        {
            Name = "",
            Slug = "INVALID SLUG"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WhenUserIsUnauthenticated_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _currentUserService.UserId = null;
        var request = new CreateTechnologyRequest
        {
            Name = "Docker",
            Slug = "docker"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateSlugForSameUser_ShouldThrowConflictExceptionAndNotUploadLogo()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        await _technologyRepository.AddAsync(Technology.Create(userId, "Docker", "docker"));

        using var stream = new MemoryStream([1, 2, 3]);
        var request = new CreateTechnologyRequest
        {
            Name = "Docker 2",
            Slug = "docker",
            Logo = new FormFile(stream, 0, stream.Length, "Logo", "docker.png")
        };

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _service.CreateAsync(request));
        Assert.Empty(_storageService.UploadedFolders);
    }

    [Fact]
    public async Task CreateAsync_WithSameSlugForDifferentUsers_ShouldSucceed()
    {
        // Arrange
        await _technologyRepository.AddAsync(Technology.Create(Guid.NewGuid(), "Docker", "docker"));

        var request = new CreateTechnologyRequest
        {
            Name = "Docker",
            Slug = "docker"
        };

        // Act
        var response = await _service.CreateAsync(request);

        // Assert
        Assert.Equal("docker", response.Slug);
    }

    [Fact]
    public async Task CreateAsync_WhenSaveChangesFails_ShouldDeleteUploadedLogo()
    {
        // Arrange
        _technologyRepository.ThrowOnSave = true;
        using var stream = new MemoryStream([1, 2, 3]);
        var request = new CreateTechnologyRequest
        {
            Name = "Redis",
            Slug = "redis",
            Logo = new FormFile(stream, 0, stream.Length, "Logo", "redis.png")
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(request));
        Assert.Single(_storageService.DeletedUrls);
        Assert.Equal("/uploads/technologies/uploaded-redis.png", _storageService.DeletedUrls[0]);
    }

    [Fact]
    public async Task GetAllAsync_WhenUserIsAuthenticated_ShouldReturnOnlyUsersTechnologiesOrderedByName()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        await _technologyRepository.AddAsync(Technology.Create(userId, "Zustand", "zustand"));
        await _technologyRepository.AddAsync(Technology.Create(userId, "Angular", "angular", logo: "/uploads/technologies/ng.png"));
        await _technologyRepository.AddAsync(Technology.Create(Guid.NewGuid(), "Other Tech", "other-tech"));

        // Act
        var result = (await _service.GetAllAsync()).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Angular", result[0].Name);
        Assert.Equal("https://localhost:59131/uploads/technologies/ng.png", result[0].Logo);
        Assert.Equal("Zustand", result[1].Name);
    }

    [Fact]
    public async Task GetAllAsync_WhenUserIsUnauthenticated_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _currentUserService.UserId = null;

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _service.GetAllAsync());
    }

    [Fact]
    public async Task GetByIdAsync_WhenTechnologyExistsForUser_ShouldReturnTechnologyWithFullLogoUrl()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var tech = Technology.Create(userId, "React", "react", "UI library", "/uploads/technologies/react.png");
        await _technologyRepository.AddAsync(tech);

        // Act
        var response = await _service.GetByIdAsync(tech.Id);

        // Assert
        Assert.Equal(tech.Id, response.Id);
        Assert.Equal("React", response.Name);
        Assert.Equal("react", response.Slug);
        Assert.Equal("UI library", response.Description);
        Assert.Equal("https://localhost:59131/uploads/technologies/react.png", response.Logo);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTechnologyBelongsToAnotherUser_ShouldThrowNotFoundException()
    {
        // Arrange
        var otherTech = Technology.Create(Guid.NewGuid(), "Private", "private");
        await _technologyRepository.AddAsync(otherTech);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(otherTech.Id));
    }

    [Fact]
    public async Task UpdateAsync_WithoutNewLogo_ShouldPreserveLogoAndUpdateTechnology()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var tech = Technology.Create(userId, "Old Name", "old-slug", "Old desc", "/uploads/technologies/existing.png");
        await _technologyRepository.AddAsync(tech);

        var request = new UpdateTechnologyRequest
        {
            Name = "  New Name  ",
            Slug = "new-slug",
            Description = "  New desc  "
        };

        // Act
        var response = await _service.UpdateAsync(tech.Id, request);

        // Assert
        Assert.Equal("New Name", response.Name);
        Assert.Equal("new-slug", response.Slug);
        Assert.Equal("New desc", response.Description);
        Assert.Equal("https://localhost:59131/uploads/technologies/existing.png", response.Logo);
        Assert.Empty(_storageService.DeletedUrls);
    }

    [Fact]
    public async Task UpdateAsync_KeepingSameSlug_ShouldSucceed()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var tech = Technology.Create(userId, "Docker", "docker");
        await _technologyRepository.AddAsync(tech);

        var request = new UpdateTechnologyRequest
        {
            Name = "Docker Engine",
            Slug = "docker"
        };

        // Act
        var response = await _service.UpdateAsync(tech.Id, request);

        // Assert
        Assert.Equal("Docker Engine", response.Name);
        Assert.Equal("docker", response.Slug);
    }

    [Fact]
    public async Task UpdateAsync_WithNewLogo_ShouldUploadNewLogoAndDeleteOldLogo()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var tech = Technology.Create(userId, "Docker", "docker", logo: "/uploads/technologies/old.png");
        await _technologyRepository.AddAsync(tech);

        using var stream = new MemoryStream([1, 2, 3]);
        var request = new UpdateTechnologyRequest
        {
            Name = "Docker",
            Slug = "docker",
            Logo = new FormFile(stream, 0, stream.Length, "Logo", "new.webp")
        };

        // Act
        var response = await _service.UpdateAsync(tech.Id, request);

        // Assert
        Assert.Equal("https://localhost:59131/uploads/technologies/uploaded-new.webp", response.Logo);
        Assert.Single(_storageService.DeletedUrls);
        Assert.Equal("/uploads/technologies/old.png", _storageService.DeletedUrls[0]);
    }

    [Fact]
    public async Task UpdateAsync_WhenSaveChangesFails_ShouldDeleteNewLogoAndKeepOldLogo()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var tech = Technology.Create(userId, "Docker", "docker", logo: "/uploads/technologies/old.png");
        await _technologyRepository.AddAsync(tech);
        _technologyRepository.ThrowOnSave = true;

        using var stream = new MemoryStream([1, 2, 3]);
        var request = new UpdateTechnologyRequest
        {
            Name = "Docker",
            Slug = "docker",
            Logo = new FormFile(stream, 0, stream.Length, "Logo", "new.png")
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateAsync(tech.Id, request));
        Assert.Single(_storageService.DeletedUrls);
        Assert.Equal("/uploads/technologies/uploaded-new.png", _storageService.DeletedUrls[0]);
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateSlugOfAnotherTechnology_ShouldThrowConflictException()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var first = Technology.Create(userId, "First", "first");
        var second = Technology.Create(userId, "Second", "second");
        await _technologyRepository.AddAsync(first);
        await _technologyRepository.AddAsync(second);

        var request = new UpdateTechnologyRequest
        {
            Name = "Second",
            Slug = "first"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _service.UpdateAsync(second.Id, request));
    }

    [Fact]
    public async Task UpdateAsync_WhenTechnologyBelongsToAnotherUser_ShouldThrowNotFoundException()
    {
        // Arrange
        var otherTech = Technology.Create(Guid.NewGuid(), "Other", "other");
        await _technologyRepository.AddAsync(otherTech);

        var request = new UpdateTechnologyRequest
        {
            Name = "Hacked",
            Slug = "hacked"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(otherTech.Id, request));
    }

    [Fact]
    public async Task DeleteAsync_WhenTechnologyExistsForUser_ShouldDeleteTechnologyAndLogo()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var tech = Technology.Create(userId, "Redis", "redis", logo: "/uploads/technologies/redis.png");
        await _technologyRepository.AddAsync(tech);

        // Act
        await _service.DeleteAsync(tech.Id);

        // Assert
        Assert.Null(await _technologyRepository.GetByIdAsync(tech.Id));
        Assert.Single(_storageService.DeletedUrls);
        Assert.Equal("/uploads/technologies/redis.png", _storageService.DeletedUrls[0]);
    }

    [Fact]
    public async Task DeleteAsync_WhenTechnologyBelongsToAnotherUser_ShouldThrowNotFoundException()
    {
        // Arrange
        var otherTech = Technology.Create(Guid.NewGuid(), "Other", "other");
        await _technologyRepository.AddAsync(otherTech);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(otherTech.Id));
        Assert.NotNull(await _technologyRepository.GetByIdAsync(otherTech.Id));
    }

    [Fact]
    public async Task DeleteAsync_WhenSaveChangesFails_ShouldNotDeleteLogo()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var tech = Technology.Create(userId, "Redis", "redis", logo: "/uploads/technologies/redis.png");
        await _technologyRepository.AddAsync(tech);
        _technologyRepository.ThrowOnSave = true;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(tech.Id));
        Assert.Empty(_storageService.DeletedUrls);
    }

    private sealed class InMemoryTechnologyRepository : ITechnologyRepository
    {
        private readonly List<Technology> _technologies = [];

        public bool ThrowOnSave { get; set; }

        public Task<Technology?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_technologies.FirstOrDefault(t => t.Id == id));

        public Task<Technology?> GetByIdAndUserIdAsync(Guid id, Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(_technologies.FirstOrDefault(t => t.Id == id && t.UserId == userId));

        public Task<IReadOnlyList<Technology>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Technology> result = _technologies
                .Where(t => t.UserId == userId)
                .OrderBy(t => t.Name)
                .ToList();

            return Task.FromResult(result);
        }

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
            Task.FromResult(_technologies.Any(t =>
                t.UserId == userId
                && t.Slug == slug
                && (!excludeTechnologyId.HasValue || t.Id != excludeTechnologyId.Value)));

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

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (ThrowOnSave)
            {
                throw new InvalidOperationException("Simulated persistence failure.");
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? UserId { get; set; } = Guid.NewGuid();
        public string? Email => "user@example.com";
        public bool IsAuthenticated => UserId.HasValue;
    }

    private sealed class FakeStorageService : IStorageService
    {
        public List<string> UploadedFolders { get; } = [];
        public List<string> DeletedUrls { get; } = [];

        public Task<StorageFileResponse> UploadAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
        {
            UploadedFolders.Add(folder);
            var url = $"/uploads/{folder}/uploaded-{file.FileName}";
            return Task.FromResult(new StorageFileResponse(
                FileName: $"uploaded-{file.FileName}",
                OriginalFileName: file.FileName,
                FileUrl: url,
                FilePath: url,
                ContentType: "image/png",
                SizeInBytes: file.Length,
                SizeInMB: 0.01,
                Extension: Path.GetExtension(file.FileName)));
        }

        public Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default)
        {
            DeletedUrls.Add(fileUrl);
            return Task.CompletedTask;
        }
    }
}

