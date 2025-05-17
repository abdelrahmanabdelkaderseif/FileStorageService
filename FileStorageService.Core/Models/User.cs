using System;
using System.Collections.Generic;

namespace FileStorageService.Core.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;
        public string[] Roles { get; set; } = new[] { UserRoles.User };

        // Navigation properties
        public virtual ICollection<FilePermission> FilePermissions { get; set; }
        public virtual ICollection<FileMetadata> OwnedFiles { get; set; }
    }

    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string FileManager = "FileManager";
        public const string User = "User";
    }

    public static class FilePermissions
    {
        public const string Upload = "file.upload";
        public const string Download = "file.download";
        public const string Delete = "file.delete";
        public const string Update = "file.update";
        public const string View = "file.view";
    }
} 