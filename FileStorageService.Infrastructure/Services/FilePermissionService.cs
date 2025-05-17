using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FileStorageService.Core.Interfaces;
using FileStorageService.Core.Models;
using FileStorageService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FileStorageService.Infrastructure.Services
{
    public class FilePermissionService : IFilePermissionService
    {
        private readonly ApplicationDbContext _context;

        public FilePermissionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> HasPermissionAsync(Guid userId, Guid fileId, FilePermissionType requiredPermission)
        {
            var permissions = await _context.FilePermissions
                .Where(p => p.UserId == userId && p.FileId == fileId)
                .Select(p => p.PermissionType)
                .ToListAsync();

            return permissions.Any(p => (p & requiredPermission) == requiredPermission);
        }

        public async Task<IEnumerable<FilePermission>> GetUserFilePermissionsAsync(Guid userId, Guid fileId)
        {
            return await _context.FilePermissions
                .Where(p => p.UserId == userId && p.FileId == fileId)
                .Include(p => p.File)
                .ToListAsync();
        }

        public async Task<bool> GrantPermissionAsync(Guid grantingUserId, Guid targetUserId, Guid fileId, FilePermissionType permission)
        {
            // Check if granting user has full control
            var hasFullControl = await HasPermissionAsync(grantingUserId, fileId, FilePermissionType.FullControl);
            if (!hasFullControl)
            {
                return false;
            }

            var existingPermission = await _context.FilePermissions
                .FirstOrDefaultAsync(p => p.UserId == targetUserId && p.FileId == fileId);

            if (existingPermission != null)
            {
                existingPermission.PermissionType |= permission;
                existingPermission.ModifiedAt = DateTime.UtcNow;
            }
            else
            {
                _context.FilePermissions.Add(new FilePermission
                {
                    Id = Guid.NewGuid(),
                    FileId = fileId,
                    UserId = targetUserId,
                    PermissionType = permission,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RevokePermissionAsync(Guid revokingUserId, Guid targetUserId, Guid fileId, FilePermissionType permission)
        {
            // Check if revoking user has full control
            var hasFullControl = await HasPermissionAsync(revokingUserId, fileId, FilePermissionType.FullControl);
            if (!hasFullControl)
            {
                return false;
            }

            var existingPermission = await _context.FilePermissions
                .FirstOrDefaultAsync(p => p.UserId == targetUserId && p.FileId == fileId);

            if (existingPermission != null)
            {
                existingPermission.PermissionType &= ~permission;
                existingPermission.ModifiedAt = DateTime.UtcNow;

                // If no permissions left, remove the entry
                if (existingPermission.PermissionType == 0)
                {
                    _context.FilePermissions.Remove(existingPermission);
                }

                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        public async Task<IEnumerable<FileMetadata>> GetAccessibleFilesAsync(Guid userId, FilePermissionType permission)
        {
            return await _context.FilePermissions
                .Where(p => p.UserId == userId && (p.PermissionType & permission) == permission)
                .Include(p => p.File)
                .Where(p => !p.File.IsDeleted)
                .Select(p => p.File)
                .ToListAsync();
        }
    }
} 