using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Common.Options;
using TaskManagement.Application.DTOs.Companies;
using TaskManagement.Application.DTOs.Storage;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Application.Validators.Companies;
using TaskManagement.Domain.Entities;
using Xunit;

namespace TaskManagement.UnitTests.Companies;

public class CompaniesServiceTests
{
    private readonly InMemoryCompanyRepository _companyRepository = new();
    private readonly FakeCurrentUserService _currentUserService = new();
    private readonly FakeStorageService _storageService = new();
    private readonly CompaniesService _service;

    public CompaniesServiceTests()
    {
        _service = new CompaniesService(
            _companyRepository,
            _currentUserService,
            _storageService,
            new CreateCompanyRequestValidator(),
            new UpdateCompanyRequestValidator(),
            Options.Create(new AppOptions { BackendUrl = "https://localhost:59131/" }));
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldCreateCompany()
    {
        // Arrange
        var request = new CreateCompanyRequest
        {
            Name = "  Acme Corp  ",
            Slug = "acme-corp",
            Description = "  Core workspace  ",
            Website = "https://acme.example.com",
            IsDefault = true
        };

        // Act
        var response = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal("Acme Corp", response.Name);
        Assert.Equal("acme-corp", response.Slug);
        Assert.Null(response.Logo);
        Assert.Equal("https://acme.example.com", response.Website);
        Assert.True(response.IsDefault);

        var saved = await _companyRepository.GetByIdAsync(response.Id);
        Assert.NotNull(saved);
        Assert.Equal(_currentUserService.UserId, saved.UserId);
        Assert.Equal("Core workspace", saved.Description);
    }

    [Fact]
    public async Task CreateAsync_WithLogo_ShouldUploadLogoAndReturnLogoUrl()
    {
        // Arrange
        using var stream = new MemoryStream([1, 2, 3, 4]);
        var logoFile = new FormFile(stream, 0, stream.Length, "Logo", "brand.webp");
        var request = new CreateCompanyRequest
        {
            Name = "Acme Corp",
            Slug = "acme-corp",
            Logo = logoFile
        };

        // Act
        var response = await _service.CreateAsync(request);

        // Assert
        Assert.Equal("https://localhost:59131/uploads/companies/uploaded-brand.webp", response.Logo);
        Assert.Single(_storageService.UploadedFolders);
        Assert.Equal("companies", _storageService.UploadedFolders[0]);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidRequest_ShouldThrowValidationException()
    {
        // Arrange
        var request = new CreateCompanyRequest
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
        var request = new CreateCompanyRequest
        {
            Name = "Acme Corp",
            Slug = "acme-corp"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateSlug_ShouldThrowConflictExceptionAndNotUploadLogo()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        await _companyRepository.AddAsync(Company.Create(userId, "Existing", "acme-corp"));

        using var stream = new MemoryStream([1, 2, 3]);
        var request = new CreateCompanyRequest
        {
            Name = "Acme Corp",
            Slug = "acme-corp",
            Logo = new FormFile(stream, 0, stream.Length, "Logo", "brand.png")
        };

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _service.CreateAsync(request));
        Assert.Empty(_storageService.UploadedFolders);
    }

    [Fact]
    public async Task CreateAsync_WithIsDefaultTrue_ShouldClearExistingDefaultCompany()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var existingDefault = Company.Create(userId, "First", "first-co", isDefault: true);
        await _companyRepository.AddAsync(existingDefault);

        var request = new CreateCompanyRequest
        {
            Name = "Second",
            Slug = "second-co",
            IsDefault = true
        };

        // Act
        var response = await _service.CreateAsync(request);

        // Assert
        Assert.True(response.IsDefault);
        Assert.False(existingDefault.IsDefault);
    }

    [Fact]
    public async Task CreateAsync_WhenSaveChangesFails_ShouldDeleteUploadedLogo()
    {
        // Arrange
        _companyRepository.ThrowOnSave = true;
        using var stream = new MemoryStream([1, 2, 3]);
        var request = new CreateCompanyRequest
        {
            Name = "Acme Corp",
            Slug = "acme-corp",
            Logo = new FormFile(stream, 0, stream.Length, "Logo", "brand.png")
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(request));
        Assert.Single(_storageService.DeletedUrls);
        Assert.Equal("/uploads/companies/uploaded-brand.png", _storageService.DeletedUrls[0]);
    }

    [Fact]
    public async Task GetAllAsync_WhenUserIsAuthenticated_ShouldReturnOnlyUsersCompaniesWithDefaultFirst()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var otherUserId = Guid.NewGuid();

        await _companyRepository.AddAsync(Company.Create(userId, "Zeta Corp", "zeta-corp", isDefault: false));
        await _companyRepository.AddAsync(Company.Create(userId, "Alpha Corp", "alpha-corp", isDefault: false));
        await _companyRepository.AddAsync(Company.Create(userId, "Default Corp", "default-corp", isDefault: true));
        await _companyRepository.AddAsync(Company.Create(otherUserId, "Other User Co", "other-user-co", isDefault: true));

        // Act
        var result = (await _service.GetAllAsync()).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Default Corp", result[0].Name);
        Assert.True(result[0].IsDefault);
        Assert.Equal("Alpha Corp", result[1].Name);
        Assert.Equal("Zeta Corp", result[2].Name);
    }

    [Fact]
    public async Task GetAllAsync_WhenUserHasNoCompanies_ShouldReturnEmptyCollection()
    {
        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Empty(result);
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
    public async Task GetByIdAsync_WhenCompanyExistsForUser_ShouldReturnCompanyWithFullLogoUrl()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var company = Company.Create(
            userId,
            "Acme Corp",
            "acme-corp",
            logo: "/uploads/companies/logo.jpg",
            website: "https://acme.example.com",
            isDefault: true);
        await _companyRepository.AddAsync(company);

        // Act
        var response = await _service.GetByIdAsync(company.Id);

        // Assert
        Assert.Equal(company.Id, response.Id);
        Assert.Equal("Acme Corp", response.Name);
        Assert.Equal("acme-corp", response.Slug);
        Assert.Equal("https://localhost:59131/uploads/companies/logo.jpg", response.Logo);
        Assert.Equal("https://acme.example.com", response.Website);
        Assert.True(response.IsDefault);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCompanyDoesNotExist_ShouldThrowNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByIdAsync_WhenCompanyBelongsToAnotherUser_ShouldThrowNotFoundException()
    {
        // Arrange
        var otherUserCompany = Company.Create(Guid.NewGuid(), "Other Co", "other-co");
        await _companyRepository.AddAsync(otherUserCompany);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(otherUserCompany.Id));
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserIsUnauthenticated_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _currentUserService.UserId = null;

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequestWithoutNewLogo_ShouldPreserveLogoAndUpdateCompany()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var company = Company.Create(
            userId,
            "Old Name",
            "old-slug",
            description: "Old desc",
            logo: "/uploads/companies/existing.png",
            website: "https://old.example.com",
            isDefault: false);
        await _companyRepository.AddAsync(company);

        var request = new UpdateCompanyRequest
        {
            Name = "  New Name  ",
            Slug = "new-slug",
            Description = "  New desc  ",
            Website = "https://new.example.com",
            IsDefault = true
        };

        // Act
        var response = await _service.UpdateAsync(company.Id, request);

        // Assert
        Assert.Equal(company.Id, response.Id);
        Assert.Equal("New Name", response.Name);
        Assert.Equal("new-slug", response.Slug);
        Assert.Equal("https://localhost:59131/uploads/companies/existing.png", response.Logo);
        Assert.Equal("https://new.example.com", response.Website);
        Assert.True(response.IsDefault);
        Assert.Equal("New desc", company.Description);
        Assert.Empty(_storageService.DeletedUrls);
    }

    [Fact]
    public async Task UpdateAsync_KeepingSameSlug_ShouldSucceed()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var company = Company.Create(userId, "Acme Corp", "acme-corp");
        await _companyRepository.AddAsync(company);

        var request = new UpdateCompanyRequest
        {
            Name = "Acme Updated",
            Slug = "acme-corp"
        };

        // Act
        var response = await _service.UpdateAsync(company.Id, request);

        // Assert
        Assert.Equal("Acme Updated", response.Name);
        Assert.Equal("acme-corp", response.Slug);
    }

    [Fact]
    public async Task UpdateAsync_WithNewLogo_ShouldUploadNewLogoAndDeleteOldLogo()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var company = Company.Create(userId, "Acme Corp", "acme-corp", logo: "/uploads/companies/old.png");
        await _companyRepository.AddAsync(company);

        using var stream = new MemoryStream([1, 2, 3]);
        var request = new UpdateCompanyRequest
        {
            Name = "Acme Corp",
            Slug = "acme-corp",
            Logo = new FormFile(stream, 0, stream.Length, "Logo", "new-logo.webp")
        };

        // Act
        var response = await _service.UpdateAsync(company.Id, request);

        // Assert
        Assert.Equal("https://localhost:59131/uploads/companies/uploaded-new-logo.webp", response.Logo);
        Assert.Single(_storageService.DeletedUrls);
        Assert.Equal("/uploads/companies/old.png", _storageService.DeletedUrls[0]);
    }

    [Fact]
    public async Task UpdateAsync_WhenSaveChangesFails_ShouldDeleteNewLogoAndKeepOldLogo()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var company = Company.Create(userId, "Acme Corp", "acme-corp", logo: "/uploads/companies/old.png");
        await _companyRepository.AddAsync(company);
        _companyRepository.ThrowOnSave = true;

        using var stream = new MemoryStream([1, 2, 3]);
        var request = new UpdateCompanyRequest
        {
            Name = "Acme Corp",
            Slug = "acme-corp",
            Logo = new FormFile(stream, 0, stream.Length, "Logo", "new-logo.png")
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateAsync(company.Id, request));
        Assert.Single(_storageService.DeletedUrls);
        Assert.Equal("/uploads/companies/uploaded-new-logo.png", _storageService.DeletedUrls[0]);
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateSlugOfAnotherCompany_ShouldThrowConflictException()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var first = Company.Create(userId, "First Co", "first-co");
        var second = Company.Create(userId, "Second Co", "second-co");
        await _companyRepository.AddAsync(first);
        await _companyRepository.AddAsync(second);

        var request = new UpdateCompanyRequest
        {
            Name = "Second Co",
            Slug = "first-co"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _service.UpdateAsync(second.Id, request));
    }

    [Fact]
    public async Task UpdateAsync_WhenCompanyBelongsToAnotherUser_ShouldThrowNotFoundException()
    {
        // Arrange
        var otherCompany = Company.Create(Guid.NewGuid(), "Other Co", "other-co");
        await _companyRepository.AddAsync(otherCompany);

        var request = new UpdateCompanyRequest
        {
            Name = "Hacked",
            Slug = "hacked"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(otherCompany.Id, request));
    }

    [Fact]
    public async Task UpdateAsync_WithIsDefaultTrue_ShouldClearOtherDefaultCompany()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var first = Company.Create(userId, "First Co", "first-co", isDefault: true);
        var second = Company.Create(userId, "Second Co", "second-co", isDefault: false);
        await _companyRepository.AddAsync(first);
        await _companyRepository.AddAsync(second);

        var request = new UpdateCompanyRequest
        {
            Name = "Second Co",
            Slug = "second-co",
            IsDefault = true
        };

        // Act
        var response = await _service.UpdateAsync(second.Id, request);

        // Assert
        Assert.True(response.IsDefault);
        Assert.False(first.IsDefault);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidRequest_ShouldThrowValidationException()
    {
        // Arrange
        var request = new UpdateCompanyRequest
        {
            Name = "",
            Slug = "INVALID SLUG"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(Guid.NewGuid(), request));
    }

    [Fact]
    public async Task UpdateAsync_WhenUserIsUnauthenticated_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _currentUserService.UserId = null;
        var request = new UpdateCompanyRequest
        {
            Name = "Valid",
            Slug = "valid"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _service.UpdateAsync(Guid.NewGuid(), request));
    }

    [Fact]
    public async Task DeleteAsync_WhenCompanyExistsForUser_ShouldDeleteCompanyAndLogo()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var company = Company.Create(userId, "Acme Corp", "acme-corp", logo: "/uploads/companies/logo.png");
        await _companyRepository.AddAsync(company);

        // Act
        await _service.DeleteAsync(company.Id);

        // Assert
        Assert.Null(await _companyRepository.GetByIdAsync(company.Id));
        Assert.Single(_storageService.DeletedUrls);
        Assert.Equal("/uploads/companies/logo.png", _storageService.DeletedUrls[0]);
    }

    [Fact]
    public async Task DeleteAsync_WhenCompanyHasNoLogo_ShouldDeleteCompanyWithoutCallingStorageDelete()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var company = Company.Create(userId, "Acme Corp", "acme-corp");
        await _companyRepository.AddAsync(company);

        // Act
        await _service.DeleteAsync(company.Id);

        // Assert
        Assert.Null(await _companyRepository.GetByIdAsync(company.Id));
        Assert.Empty(_storageService.DeletedUrls);
    }

    [Fact]
    public async Task DeleteAsync_WhenCompanyDoesNotExist_ShouldThrowNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAsync_WhenCompanyBelongsToAnotherUser_ShouldThrowNotFoundException()
    {
        // Arrange
        var otherCompany = Company.Create(Guid.NewGuid(), "Other Co", "other-co");
        await _companyRepository.AddAsync(otherCompany);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(otherCompany.Id));
        Assert.NotNull(await _companyRepository.GetByIdAsync(otherCompany.Id));
    }

    [Fact]
    public async Task DeleteAsync_WhenUserIsUnauthenticated_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _currentUserService.UserId = null;

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _service.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAsync_WhenSaveChangesFails_ShouldNotDeleteLogo()
    {
        // Arrange
        var userId = _currentUserService.UserId!.Value;
        var company = Company.Create(userId, "Acme Corp", "acme-corp", logo: "/uploads/companies/logo.png");
        await _companyRepository.AddAsync(company);
        _companyRepository.ThrowOnSave = true;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(company.Id));
        Assert.Empty(_storageService.DeletedUrls);
    }

    private sealed class InMemoryCompanyRepository : ICompanyRepository
    {
        private readonly List<Company> _companies = [];

        public bool ThrowOnSave { get; set; }

        public Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_companies.FirstOrDefault(c => c.Id == id));

        public Task<Company?> GetByIdAndUserIdAsync(Guid id, Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(_companies.FirstOrDefault(c => c.Id == id && c.UserId == userId));

        public Task<IReadOnlyList<Company>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Company> result = _companies
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.IsDefault)
                .ThenBy(c => c.Name)
                .ToList();

            return Task.FromResult(result);
        }

        public Task<bool> ExistsBySlugAsync(Guid userId, string slug, Guid? excludeCompanyId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(_companies.Any(c =>
                c.UserId == userId
                && c.Slug == slug
                && (!excludeCompanyId.HasValue || c.Id != excludeCompanyId.Value)));

        public Task ClearDefaultForUserAsync(Guid userId, Guid? excludeCompanyId = null, CancellationToken cancellationToken = default)
        {
            foreach (var company in _companies.Where(c =>
                c.UserId == userId
                && c.IsDefault
                && (!excludeCompanyId.HasValue || c.Id != excludeCompanyId.Value)))
            {
                company.SetDefault(false);
            }

            return Task.CompletedTask;
        }

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

