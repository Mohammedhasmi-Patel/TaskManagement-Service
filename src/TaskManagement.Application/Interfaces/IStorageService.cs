using Microsoft.AspNetCore.Http;
using TaskManagement.Application.DTOs.Storage;

namespace TaskManagement.Application.Interfaces;

public interface IStorageService
{
    Task<StorageFileResponse> UploadAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string fileUrl,
        CancellationToken cancellationToken = default);
}

