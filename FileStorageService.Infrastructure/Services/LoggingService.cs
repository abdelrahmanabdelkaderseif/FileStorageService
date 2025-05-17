using System;
using System.Threading.Tasks;
using FileStorageService.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace FileStorageService.Infrastructure.Services
{
    public class LoggingService : ILoggingService
    {
        private readonly ILogger<LoggingService> _logger;

        public LoggingService(ILogger<LoggingService> logger)
        {
            _logger = logger;
        }

        public Task LogInformationAsync(string message, string userId = null, string action = null)
        {
            var logData = CreateLogData(message, userId, action);
            _logger.LogInformation("{Message} {@LogData}", message, logData);
            return Task.CompletedTask;
        }

        public Task LogWarningAsync(string message, string userId = null, string action = null)
        {
            var logData = CreateLogData(message, userId, action);
            _logger.LogWarning("{Message} {@LogData}", message, logData);
            return Task.CompletedTask;
        }

        public Task LogErrorAsync(string message, Exception ex = null, string userId = null, string action = null)
        {
            var logData = CreateLogData(message, userId, action);
            if (ex != null)
            {
                _logger.LogError(ex, "{Message} {@LogData}", message, logData);
            }
            else
            {
                _logger.LogError("{Message} {@LogData}", message, logData);
            }
            return Task.CompletedTask;
        }

        public Task LogSecurityEventAsync(string message, string userId = null, string action = null)
        {
            var logData = CreateLogData(message, userId, action);
            logData["EventType"] = "Security";
            _logger.LogInformation("{Message} {@LogData}", message, logData);
            return Task.CompletedTask;
        }

        public Task LogPerformanceMetricAsync(string operation, long durationMs, string userId = null)
        {
            var logData = new Dictionary<string, object>
            {
                ["Operation"] = operation,
                ["DurationMs"] = durationMs,
                ["Timestamp"] = DateTime.UtcNow,
                ["MetricType"] = "Performance"
            };

            if (!string.IsNullOrEmpty(userId))
            {
                logData["UserId"] = userId;
            }

            _logger.LogInformation("Performance metric: {Operation} took {DurationMs}ms {@LogData}", 
                operation, durationMs, logData);
            return Task.CompletedTask;
        }

        private Dictionary<string, object> CreateLogData(string message, string userId = null, string action = null)
        {
            var logData = new Dictionary<string, object>
            {
                ["Timestamp"] = DateTime.UtcNow,
                ["Message"] = message
            };

            if (!string.IsNullOrEmpty(userId))
            {
                logData["UserId"] = userId;
            }

            if (!string.IsNullOrEmpty(action))
            {
                logData["Action"] = action;
            }

            return logData;
        }
    }
} 