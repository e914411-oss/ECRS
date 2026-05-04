using System.ComponentModel.DataAnnotations;

namespace ECRS_API.Models
{
    public class AppUser
    {
        public string Id { get; set; } = default!;

        public string UserName { get; set; } = default!;   // 帳號
        public string PasswordHash { get; set; } = default!; // 密碼雜湊

        public string DisplayName { get; set; } = default!;
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAtUtc { get; set; }
    }
}
