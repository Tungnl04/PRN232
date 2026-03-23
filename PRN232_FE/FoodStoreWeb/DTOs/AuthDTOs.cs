namespace FoodStoreWeb.DTOs
{
    public class UserDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Role { get; set; }
        public int Active { get; set; }
    }

    public class LoginRequestDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public UserDto? User { get; set; }
        public string? Token { get; set; } // Nếu API dùng JWT
    }

    public class UpdateUserDto
    {
        public string Name { get; set; } = string.Empty;
    }

    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
