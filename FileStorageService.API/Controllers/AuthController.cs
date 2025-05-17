using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using FileStorageService.Core.Interfaces;
using FileStorageService.Core.Models;

namespace FileStorageService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            var response = await _authService.LoginAsync(request);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            var response = await _authService.RegisterAsync(request);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPost("validate-token")]
        public async Task<ActionResult<bool>> ValidateToken([FromBody] string token)
        {
            var isValid = await _authService.ValidateTokenAsync(token);
            return Ok(isValid);
        }
    }

    //public class LoginRequest
    //{
    //    public string Username { get; set; }
    //    public string Password { get; set; }
    //}

    //public class RegisterRequest
    //{
    //    public string Username { get; set; }
    //    public string Password { get; set; }
    //    public string Email { get; set; }
    //}
} 