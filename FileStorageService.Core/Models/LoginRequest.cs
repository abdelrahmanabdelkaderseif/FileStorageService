using System.ComponentModel.DataAnnotations;

namespace FileStorageService.Core.Models
{
    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
} 