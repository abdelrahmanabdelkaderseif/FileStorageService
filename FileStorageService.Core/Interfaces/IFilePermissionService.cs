using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FileStorageService.Core.Models;

namespace FileStorageService.Core.Interfaces
{
    public interface IFilePermissionService
    {
        Task<bool> HasPermissionAsync(Guid userId, Guid fileId, FilePermissionType requiredPermission);
        Task<IEnumerable<FilePermission>> GetUserFilePermissionsAsync(Guid userId, Guid fileId);
        Task<bool> GrantPermissionAsync(Guid grantingUserId, Guid targetUserId, Guid fileId, FilePermissionType permission);
        Task<bool> RevokePermissionAsync(Guid revokingUserId, Guid targetUserId, Guid fileId, FilePermissionType permission);
        Task<IEnumerable<FileMetadata>> GetAccessibleFilesAsync(Guid userId, FilePermissionType permission);
    }
} 