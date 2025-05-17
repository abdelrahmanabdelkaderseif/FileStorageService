using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FileStorageService.Core.Interfaces;
using FileStorageService.Core.Models;
using System.Security.Claims;

namespace FileStorageService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FilePermissionController : ControllerBase
    {
        private readonly IFilePermissionService _permissionService;

        public FilePermissionController(IFilePermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        [HttpGet("files/{fileId}/permissions")]
        public async Task<IActionResult> GetFilePermissions(Guid fileId)
        {
            var userId = GetCurrentUserId();
            var permissions = await _permissionService.GetUserFilePermissionsAsync(userId, fileId);
            return Ok(permissions);
        }

        [HttpPost("files/{fileId}/permissions")]
        public async Task<IActionResult> GrantPermission(Guid fileId, [FromBody] GrantPermissionRequest request)
        {
            var grantingUserId = GetCurrentUserId();
            var success = await _permissionService.GrantPermissionAsync(
                grantingUserId,
                request.TargetUserId,
                fileId,
                request.Permission);

            if (!success)
            {
                return Forbid();
            }

            return Ok();
        }

        [HttpDelete("files/{fileId}/permissions")]
        public async Task<IActionResult> RevokePermission(Guid fileId, [FromBody] RevokePermissionRequest request)
        {
            var revokingUserId = GetCurrentUserId();
            var success = await _permissionService.RevokePermissionAsync(
                revokingUserId,
                request.TargetUserId,
                fileId,
                request.Permission);

            if (!success)
            {
                return Forbid();
            }

            return Ok();
        }

        [HttpGet("files")]
        public async Task<IActionResult> GetAccessibleFiles([FromQuery] FilePermissionType permission)
        {
            var userId = GetCurrentUserId();
            var files = await _permissionService.GetAccessibleFilesAsync(userId, permission);
            return Ok(files);
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                throw new UnauthorizedAccessException("User ID not found in claims");
            }
            return userId;
        }
    }

    public class GrantPermissionRequest
    {
        public Guid TargetUserId { get; set; }
        public FilePermissionType Permission { get; set; }
    }

    public class RevokePermissionRequest
    {
        public Guid TargetUserId { get; set; }
        public FilePermissionType Permission { get; set; }
    }
} 