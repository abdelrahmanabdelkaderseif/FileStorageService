using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FileStorageService.Core.Models;
using Microsoft.AspNetCore.Http;

namespace FileStorageService.Core.Interfaces
{
    public interface IFileStorageService
    {
        Task<FileMetadata> UploadFileAsync(IFormFile file, string uploadedBy, Dictionary<string, string> customMetadata = null);
        Task<(Stream FileStream, FileMetadata Metadata)> DownloadFileAsync(Guid fileId);
        Task<bool> DeleteFileAsync(Guid fileId);
        Task<FileMetadata> GetFileMetadataAsync(Guid fileId);
        Task<IEnumerable<FileMetadata>> SearchFilesAsync(string searchTerm);
        Task<FileMetadata> UpdateFileMetadataAsync(Guid fileId, Dictionary<string, string> metadata);
    }
} 