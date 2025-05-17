using System;
using System.Linq;
using System.Threading.Tasks;
using FileStorageService.Core.Interfaces;
using FileStorageService.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace FileStorageService.API.Authorization
{
    public class FilePermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }

        public FilePermissionRequirement(string permission)
        {
            Permission = permission;
        }
    }

    public class FilePermissionHandler : AuthorizationHandler<FilePermissionRequirement>
    {
        private readonly IAuthService _authService;

        public FilePermissionHandler(IAuthService authService)
        {
            _authService = authService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            FilePermissionRequirement requirement)
        {
            if (context.User?.Identity?.Name == null)
            {
                return;
            }

            var user = await _authService.GetUserFromTokenAsync(context.User.Identity.Name);
            if (user == null)
            {
                return;
            }

            // Admin has all permissions
            if (user.Roles.Contains(UserRoles.Admin))
            {
                context.Succeed(requirement);
                return;
            }

            // File Manager has all file operations permissions
            if (user.Roles.Contains(UserRoles.FileManager))
            {
                if (requirement.Permission.StartsWith("file."))
                {
                    context.Succeed(requirement);
                }
                return;
            }

            // Regular users have limited permissions
            switch (requirement.Permission)
            {
                case FilePermissions.Upload:
                    // Users can upload files
                    context.Succeed(requirement);
                    break;

                case FilePermissions.Download:
                case FilePermissions.Delete:
                case FilePermissions.Update:
                case FilePermissions.View:
                    // Check if user owns the file
                    var fileId = GetFileIdFromContext(context);
                    if (fileId.HasValue && user.OwnedFiles.Any(f => f.Id == fileId.Value))
                    {
                        context.Succeed(requirement);
                    }
                    // Check if user has permission through FilePermissions
                    else if (fileId.HasValue && user.FilePermissions.Any(fp => fp.FileId == fileId.Value))
                    {
                        context.Succeed(requirement);
                    }
                    break;
            }
        }

        private Guid? GetFileIdFromContext(AuthorizationHandlerContext context)
        {
            // Try to get fileId from the current request
            if (context.Resource is HttpContext httpContext)
            {
                var routeData = httpContext.Request.RouteValues;
                if (routeData.ContainsKey("fileId") && Guid.TryParse(routeData["fileId"]?.ToString(), out Guid fileId))
                {
                    return fileId;
                }
            }
            return null;
        }
    }
} 