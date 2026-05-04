namespace ECRS_API.DTOs
{
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public string? Token { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }

        public object? User { get; set; }
    }

    public class UserProfile
    {
        public required string Id { get; set; }
        public string Username { get; set; } = default!;
        public string DisplayName { get; set; } = default!;
    }
}
