using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.DTOs.Storage;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Application.Services;

public class StorageService(IWebHostEnvironment environment) : IStorageService
{
    private const long MaxFileSizeInMb = 5;
    private const long MaxFileSize = MaxFileSizeInMb * 1024 * 1024;
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    private readonly IWebHostEnvironment _environment = environment;

    public Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return Task.CompletedTask;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var pathToDelete = Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri)
            ? uri.AbsolutePath
            : fileUrl;

        var webRootPath = GetWebRootPath();
        var relativePath = pathToDelete
            .TrimStart('/', '\\')
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(webRootPath, relativePath));
        var normalizedRoot = Path.GetFullPath(webRootPath);

        if (!fullPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Invalid file path.");
        }

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public async Task<StorageFileResponse> UploadAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
            throw new BadRequestException("File is required.");
        
        if (file.Length > MaxFileSize)
            throw new BadRequestException($"File size must be {MaxFileSizeInMb} MB or less.");
            
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            throw new BadRequestException($"The file must be one of the following types: {string.Join(", ", AllowedExtensions)}");
        
        if (string.IsNullOrWhiteSpace(folder))
            throw new BadRequestException("Invalid folder path.");

        var safeFolder = folder.Trim().Trim('/', '\\');
        if (string.IsNullOrWhiteSpace(safeFolder) || safeFolder.Contains("..", StringComparison.Ordinal))
            throw new BadRequestException("Invalid folder path.");

        var originalFileName = Path.GetFileName(file.FileName);
        var contentType = file.ContentType ?? "application/octet-stream";
        var fileName = $"{Guid.NewGuid()}{extension}";
        var sizeInBytes = file.Length;
        var sizeInMB = Math.Round(sizeInBytes / (1024d * 1024d), 2);

        var uploadsDir = Path.Combine(GetWebRootPath(), "uploads", safeFolder);
        Directory.CreateDirectory(uploadsDir);

        var uploadFilePath = Path.Combine(uploadsDir, fileName);
        var fileUrl = $"/uploads/{safeFolder.Replace('\\', '/')}/{fileName}";

        await using (var stream = new FileStream(uploadFilePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        return new StorageFileResponse(
            FileName: fileName,
            OriginalFileName: originalFileName,
            FileUrl: fileUrl,
            FilePath: uploadFilePath,
            ContentType: contentType,
            SizeInBytes: sizeInBytes,
            SizeInMB: sizeInMB,
            Extension: extension);
    }

    private string GetWebRootPath()
    {
        return string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.Combine(_environment.ContentRootPath, "wwwroot")
            : _environment.WebRootPath;
    }
}
