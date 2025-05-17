using System;
using System.Threading.Tasks;

namespace FileStorageService.Core.Interfaces
{
    public interface ILoggingService
    {
        Task LogInformationAsync(string message, string userId = null, string action = null);
        Task LogWarningAsync(string message, string userId = null, string action = null);
        Task LogErrorAsync(string message, Exception ex = null, string userId = null, string action = null);
        Task LogSecurityEventAsync(string message, string userId = null, string action = null);
        Task LogPerformanceMetricAsync(string operation, long durationMs, string userId = null);
    }
} 