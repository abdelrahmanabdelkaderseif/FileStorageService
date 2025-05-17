using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FileStorageService.Core.Interfaces;
using FileStorageService.Core.Models;
using FileStorageService.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FileStorageService.Infrastructure.Services
{
    public class FileStorageImplementation : IFileStorageService
    {
        private readonly string _storageBasePath;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public FileStorageImplementation(IConfiguration configuration, ApplicationDbContext context)
        {
            _configuration = configuration;
            _context = context;
            _storageBasePath = _configuration["FileStorage:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "FileStorage");
            
            // Ensure storage directory exists
            if (!Directory.Exists(_storageBasePath))
            {
                Directory.CreateDirectory(_storageBasePath);
            }
        }

        public async Task<FileMetadata> UploadFileAsync(IFormFile file, string uploadedBy, Dictionary<string, string> customMetadata = null)
        {
            customMetadata ??= new Dictionary<string, string>();
            
            var fileId = Guid.NewGuid();
            var fileName = Path.GetFileName(file.FileName);
            var filePath = Path.Combine(_storageBasePath, fileId.ToString());

            var metadata = new FileMetadata
            {
                Id = fileId,
                FileName = fileName,
                ContentType = file.ContentType,
                Size = file.Length,
                Path = filePath,
                UploadDate = DateTime.UtcNow,
                UploadedBy = uploadedBy,
                Version = "1.0",
                CustomMetadata = customMetadata
            };

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _context.Files.Add(metadata);
            await _context.SaveChangesAsync();

            return metadata;
        }

        public async Task<(Stream FileStream, FileMetadata Metadata)> DownloadFileAsync(Guid fileId)
        {
            var metadata = await _context.Files
                .FirstOrDefaultAsync(f => f.Id == fileId && !f.IsDeleted);

            if (metadata == null)
            {
                throw new FileNotFoundException($"File with ID {fileId} not found.");
            }

            var stream = new FileStream(metadata.Path, FileMode.Open, FileAccess.Read);
            return (stream, metadata);
        }

        public async Task<bool> DeleteFileAsync(Guid fileId)
        {
            var metadata = await _context.Files
                .FirstOrDefaultAsync(f => f.Id == fileId && !f.IsDeleted);

            if (metadata == null)
            {
                return false;
            }

            if (File.Exists(metadata.Path))
            {
                File.Delete(metadata.Path);
            }

            metadata.IsDeleted = true;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<FileMetadata> GetFileMetadataAsync(Guid fileId)
        {
            var metadata = await _context.Files
                .FirstOrDefaultAsync(f => f.Id == fileId && !f.IsDeleted);

            if (metadata == null)
            {
                throw new FileNotFoundException($"File with ID {fileId} not found.");
            }

            return metadata;
        }

        public async Task<IEnumerable<FileMetadata>> SearchFilesAsync(string searchTerm)
        {
            return await _context.Files
                .Where(f => !f.IsDeleted && f.FileName.Contains(searchTerm))
                .ToListAsync();
        }

        public async Task<FileMetadata> UpdateFileMetadataAsync(Guid fileId, Dictionary<string, string> metadata)
        {
            var existingMetadata = await _context.Files
                .FirstOrDefaultAsync(f => f.Id == fileId && !f.IsDeleted);

            if (existingMetadata == null)
            {
                throw new FileNotFoundException($"File with ID {fileId} not found.");
            }

            existingMetadata.CustomMetadata = metadata;
            await _context.SaveChangesAsync();

            return existingMetadata;
        }
    }
} 