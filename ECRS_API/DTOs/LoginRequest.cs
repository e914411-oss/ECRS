using System.ComponentModel.DataAnnotations;

namespace ECRS_API.DTOs
{
    public class LoginRequest
    {
        [Required, MaxLength(50)]
        public string UserId { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Password { get; set; } = string.Empty;
    }
}
