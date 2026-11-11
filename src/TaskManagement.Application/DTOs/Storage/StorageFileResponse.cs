using System;
using System.Collections.Generic;
using System.Text;

namespace TaskManagement.Application.DTOs.Storage;

public record StorageFileResponse(
    string FileName,
    string OriginalFileName,
    string FileUrl,
    string FilePath,
    string ContentType,
    long SizeInBytes,
    double SizeInMB,
    string Extension
);
