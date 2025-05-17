using System;
using System.Threading.Tasks;
using FileStorageService.Core.Interfaces;
using FileStorageService.Core.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace FileStorageService.API.Middleware
{
    public class FilePermissionMiddleware
    {
        private readonly RequestDelegate _next;

        public FilePermissionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IFilePermissionService permissionService)
        {
            var endpoint = context.GetEndpoint();
            if (endpoint == null)
            {
                await _next(context);
                return;
            }

            var fileIdString = context.Request.RouteValues["fileId"]?.ToString();
            if (string.IsNullOrEmpty(fileIdString) || !Guid.TryParse(fileIdString, out Guid fileId))
            {
                await _next(context);
                return;
            }

            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            // Determine required permission based on HTTP method
            var requiredPermission = GetRequiredPermission(context.Request.Method);
            
            var hasPermission = await permissionService.HasPermissionAsync(userId, fileId, requiredPermission);
            if (!hasPermission)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            await _next(context);
        }

        private FilePermissionType GetRequiredPermission(string httpMethod)
        {
            return httpMethod.ToUpper() switch
            {
                "GET" => FilePermissionType.Read,
                "POST" => FilePermissionType.Write,
                "PUT" => FilePermissionType.Write,
                "DELETE" => FilePermissionType.Delete,
                _ => FilePermissionType.Read
            };
        }
    }
} 