using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using FileStorageService.Core.Interfaces;
using FileStorageService.Core.Models;
using FileStorageService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using BC = BCrypt.Net.BCrypt;

namespace FileStorageService.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILoggingService _loggingService;

        public AuthService(
            ApplicationDbContext context, 
            IConfiguration configuration,
            ILoggingService loggingService)
        {
            _context = context;
            _configuration = configuration;
            _loggingService = loggingService;
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (user == null || !BC.Verify(request.Password, user.PasswordHash))
            {
                await _loggingService.LogSecurityEventAsync(
                    "Failed login attempt",
                    null,
                    "Login");
                    
                return new AuthResponse 
                { 
                    Success = false,
                    Message = "Invalid email or password"
                };
            }

            // Update LastLoginAt
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            var expiresAt = DateTime.UtcNow.AddHours(24);

            await _loggingService.LogSecurityEventAsync(
                "Successful login",
                user.Id.ToString(),
                "Login");

            return new AuthResponse
            {
                Success = true,
                Token = token,
                ExpiresAt = expiresAt,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName
                }
            };
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower()))
            {
                await _loggingService.LogSecurityEventAsync(
                    "Registration failed - Email already exists",
                    null,
                    "Register");
                    
                return new AuthResponse 
                { 
                    Success = false,
                    Message = "Email already exists"
                };
            }

            var passwordHash = BC.HashPassword(request.Password);
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FullName = request.FullName,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                Roles = new[] { UserRoles.User }
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            var expiresAt = DateTime.UtcNow.AddHours(24);

            await _loggingService.LogSecurityEventAsync(
                "New user registered",
                user.Id.ToString(),
                "Register");

            return new AuthResponse
            {
                Success = true,
                Token = token,
                ExpiresAt = expiresAt,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName
                }
            };
        }

        public string GenerateJwtToken(User user)
        {
            if (string.IsNullOrEmpty(_configuration["Jwt:Secret"]))
            {
                var error = "JWT secret is not configured";
                _loggingService.LogErrorAsync(error).Wait();
                throw new InvalidOperationException(error);
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Role, string.Join(",", user.Roles))
                }),
                Expires = DateTime.UtcNow.AddHours(24),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                await _loggingService.LogSecurityEventAsync(
                    "Token validation failed - Empty token",
                    null,
                    "ValidateToken");
                return false;
            }

            if (string.IsNullOrEmpty(_configuration["Jwt:Secret"]))
            {
                await _loggingService.LogErrorAsync(
                    "Token validation failed - JWT secret not configured");
                return false;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"]);

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                await _loggingService.LogSecurityEventAsync(
                    "Token validated successfully",
                    null,
                    "ValidateToken");
                return true;
            }
            catch (Exception ex)
            {
                await _loggingService.LogSecurityEventAsync(
                    "Token validation failed",
                    null,
                    "ValidateToken");
                await _loggingService.LogErrorAsync(
                    "Token validation error",
                    ex);
                return false;
            }
        }

        public async Task<bool> HasPermissionAsync(string token, string permission, Guid? fileId = null)
        {
            var user = await GetUserFromTokenAsync(token);
            if (user == null) return false;

            // Admin has all permissions
            if (user.Roles.Contains(UserRoles.Admin)) return true;

            // File Manager has all file operations permissions
            if (user.Roles.Contains(UserRoles.FileManager)) return true;

            // For file-specific permissions
            if (fileId.HasValue)
            {
                var isOwner = await _context.Files
                    .AnyAsync(f => f.Id == fileId.Value && f.UploadedBy == user.Email);

                if (isOwner) return true;

                var hasPermission = await _context.FilePermissions
                    .AnyAsync(fp => fp.FileId == fileId.Value && fp.UserId == user.Id);

                return hasPermission;
            }

            // For general permissions (like upload)
            return permission == FilePermissions.Upload;
        }

        public async Task<User> GetUserFromTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(_configuration["Jwt:Secret"]))
                return null;

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"]);

                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                    return null;

                return await _context.Users
                    .Include(u => u.FilePermissions)
                    .Include(u => u.OwnedFiles)
                    .FirstOrDefaultAsync(u => u.Id == userGuid && u.IsActive);
            }
            catch
            {
                return null;
            }
        }
    }
} 