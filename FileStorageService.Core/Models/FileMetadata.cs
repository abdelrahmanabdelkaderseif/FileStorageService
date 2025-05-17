using System;
using System.Collections.Generic;

namespace FileStorageService.Core.Models
{
    public class FileMetadata
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long Size { get; set; }
        public string Path { get; set; }
        public DateTime UploadDate { get; set; }
        public string UploadedBy { get; set; }
        public string Version { get; set; }
        public bool IsDeleted { get; set; }
        public Dictionary<string, string> CustomMetadata { get; set; }

        // Navigation property for permissions
        public virtual ICollection<FilePermission> Permissions { get; set; } = new List<FilePermission>();
    }
} 