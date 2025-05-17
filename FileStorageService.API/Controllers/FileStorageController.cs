using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FileStorageService.Core.Interfaces;
using FileStorageService.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FileStorageService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]  // Require authentication for all endpoints
    public class FileStorageController : ControllerBase
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly IAuthService _authService;

        public FileStorageController(IFileStorageService fileStorageService, IAuthService authService)
        {
            _fileStorageService = fileStorageService;
            _authService = authService;
        }

        [HttpPost("upload")]
        public async Task<ActionResult<FileMetadata>> UploadFile(
            IFormFile file,
            [FromHeader(Name = "X-Uploaded-By")] string uploadedBy,
            [FromBody] Dictionary<string, string> customMetadata = null)
        {
            var token = GetTokenFromHeader();
            if (!await _authService.HasPermissionAsync(token, FilePermissions.Upload))
                return Forbid();

            if (file == null || file.Length == 0)
                return BadRequest("No file was uploaded.");

            try
            {
                var result = await _fileStorageService.UploadFileAsync(file, uploadedBy, customMetadata);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("download/{fileId}")]
        public async Task<IActionResult> DownloadFile(Guid fileId)
        {
            var token = GetTokenFromHeader();
            if (!await _authService.HasPermissionAsync(token, FilePermissions.Download, fileId))
                return Forbid();

            try
            {
                var (fileStream, metadata) = await _fileStorageService.DownloadFileAsync(fileId);
                return File(fileStream, metadata.ContentType, metadata.FileName);
            }
            catch (FileNotFoundException)
            {
                return NotFound($"File with ID {fileId} not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("{fileId}")]
        public async Task<IActionResult> DeleteFile(Guid fileId)
        {
            var token = GetTokenFromHeader();
            if (!await _authService.HasPermissionAsync(token, FilePermissions.Delete, fileId))
                return Forbid();

            try
            {
                var result = await _fileStorageService.DeleteFileAsync(fileId);
                if (result)
                    return Ok($"File with ID {fileId} was deleted successfully.");
                return NotFound($"File with ID {fileId} not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("metadata/{fileId}")]
        public async Task<ActionResult<FileMetadata>> GetFileMetadata(Guid fileId)
        {
            var token = GetTokenFromHeader();
            if (!await _authService.HasPermissionAsync(token, FilePermissions.View, fileId))
                return Forbid();

            try
            {
                var metadata = await _fileStorageService.GetFileMetadataAsync(fileId);
                return Ok(metadata);
            }
            catch (FileNotFoundException)
            {
                return NotFound($"File with ID {fileId} not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<FileMetadata>>> SearchFiles([FromQuery] string searchTerm)
        {
            var token = GetTokenFromHeader();
            if (!await _authService.HasPermissionAsync(token, FilePermissions.View))
                return Forbid();

            try
            {
                var results = await _fileStorageService.SearchFilesAsync(searchTerm);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("{fileId}/metadata")]
        public async Task<ActionResult<FileMetadata>> UpdateMetadata(
            Guid fileId,
            [FromBody] Dictionary<string, string> metadata)
        {
            var token = GetTokenFromHeader();
            if (!await _authService.HasPermissionAsync(token, FilePermissions.Update, fileId))
                return Forbid();

            try
            {
                var result = await _fileStorageService.UpdateFileMetadataAsync(fileId, metadata);
                return Ok(result);
            }
            catch (FileNotFoundException)
            {
                return NotFound($"File with ID {fileId} not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private string GetTokenFromHeader()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return null;

            return authHeader.Substring("Bearer ".Length);
        }
    }
} 