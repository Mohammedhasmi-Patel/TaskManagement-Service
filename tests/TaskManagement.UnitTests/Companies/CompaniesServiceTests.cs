using Microsoft.AspNetCore.Http;
using TaskManagement.Application.Common.Exceptions;
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
            new CreateCompanyRequestValidator());
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
        Assert.Equal("/uploads/companies/uploaded-brand.webp", response.Logo);
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

    private sealed class InMemoryCompanyRepository : ICompanyRepository
    {
        private readonly List<Company> _companies = [];

        public bool ThrowOnSave { get; set; }

        public Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_companies.FirstOrDefault(c => c.Id == id));

        public Task<bool> ExistsBySlugAsync(Guid userId, string slug, CancellationToken cancellationToken = default) =>
            Task.FromResult(_companies.Any(c => c.UserId == userId && c.Slug == slug));

        public Task ClearDefaultForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            foreach (var company in _companies.Where(c => c.UserId == userId && c.IsDefault))
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

