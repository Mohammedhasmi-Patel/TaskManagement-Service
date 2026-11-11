using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using TaskManagement.Application.DTOs.Storage;

namespace TaskManagement.Application.Interfaces;

public interface IStorageService
{
    public Task<StorageFileResponse> UploadAsync(IFormFile file,string folder,CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string fileUrl,
        CancellationToken cancellationToken = default);

}

