using System.Diagnostics;
using System.Threading.Tasks;
using FileStorageService.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FileStorageService.API.Middleware
{
    public class PerformanceMonitoringMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILoggingService _loggingService;

        public PerformanceMonitoringMiddleware(RequestDelegate next, ILoggingService loggingService)
        {
            _next = next;
            _loggingService = loggingService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                var userId = context.User?.FindFirst("sub")?.Value;
                var operation = $"{context.Request.Method} {context.Request.Path}";
                
                await _loggingService.LogPerformanceMetricAsync(
                    operation,
                    stopwatch.ElapsedMilliseconds,
                    userId);
            }
        }
    }
} 