using Microsoft.AspNetCore.Builder;
using FileStorageService.API.Middleware;

namespace FileStorageService.API.Extensions
{
    public static class PerformanceMonitoringExtensions
    {
        public static IApplicationBuilder UsePerformanceMonitoring(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<PerformanceMonitoringMiddleware>();
        }
    }
} 