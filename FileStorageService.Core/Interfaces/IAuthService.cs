using System;
using System.Threading.Tasks;
using FileStorageService.Core.Models;

namespace FileStorageService.Core.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<bool> ValidateTokenAsync(string token);
        string GenerateJwtToken(User user);
        Task<bool> HasPermissionAsync(string token, string permission, Guid? fileId = null);
        Task<User> GetUserFromTokenAsync(string token);
    }
} 