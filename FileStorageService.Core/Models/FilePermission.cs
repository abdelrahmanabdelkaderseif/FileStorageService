using System;

namespace FileStorageService.Core.Models
{
    public class FilePermission
    {
        public Guid Id { get; set; }
        public Guid FileId { get; set; }
        public Guid UserId { get; set; }
        public FilePermissionType PermissionType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }

        // Navigation properties
        public virtual FileMetadata File { get; set; }
        public virtual User User { get; set; }
    }
} 