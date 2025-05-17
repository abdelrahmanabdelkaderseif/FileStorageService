using Microsoft.AspNetCore.Builder;
using FileStorageService.API.Middleware;

namespace FileStorageService.API.Extensions
{
    public static class FilePermissionMiddlewareExtensions
    {
        public static IApplicationBuilder UseFilePermissions(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<FilePermissionMiddleware>();
        }
    }
} 