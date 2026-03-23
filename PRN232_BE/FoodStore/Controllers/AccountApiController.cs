using FoodStoreAPI.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FoodStoreAPI.Controllers
{
    [Route("AdminApi")]
    [ApiController]
    public class AccountApiController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<AccountApiController> _logger;

        public AccountApiController(IUserService userService, ILogger<AccountApiController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        // -------------------------------------------------------
        // POST AdminApi/auth/login
        // Body: { "username": "...", "password": "..." }
        // -------------------------------------------------------
        [HttpPost("auth/login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { success = false, message = "Tên đăng nhập và mật khẩu không được để trống." });
            }

            try
            {
                var isValid = await _userService.ValidateUserAsync(request.Username, request.Password);
                if (!isValid)
                {
                    _logger.LogWarning("Đăng nhập thất bại cho username: {Username}", request.Username);
                    return Unauthorized(new { success = false, message = "Tên đăng nhập hoặc mật khẩu không đúng." });
                }

                var user = await _userService.GetUserByUsernameAsync(request.Username);
                if (user == null || user.Active != true)
                {
                    return Unauthorized(new { success = false, message = "Tài khoản không tồn tại hoặc đã bị khoá." });
                }

                _logger.LogInformation("Đăng nhập thành công cho username: {Username}", request.Username);

                return Ok(new
                {
                    success = true,
                    message = "Đăng nhập thành công.",
                    user = new
                    {
                        id       = (decimal)user.Id,
                        name     = user.Name,
                        username = user.Username,
                        role     = user.Role ?? "staff"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đăng nhập cho username: {Username}", request.Username);
                return StatusCode(500, new { success = false, message = "Lỗi máy chủ, vui lòng thử lại sau." });
            }
        }

        // -------------------------------------------------------
        // POST AdminApi/users/{id}/change-password
        // Body: { "currentPassword": "...", "newPassword": "..." }
        // -------------------------------------------------------
        [HttpPost("users/{id}/change-password")]
        public async Task<IActionResult> ChangePassword(decimal id, [FromBody] ChangePasswordRequest request)
        {
            if (request == null
                || string.IsNullOrWhiteSpace(request.CurrentPassword)
                || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest(new { success = false, message = "Mật khẩu hiện tại và mật khẩu mới không được để trống." });
            }

            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy người dùng." });
                }

                // Xác minh mật khẩu hiện tại
                var isCurrentValid = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash);
                if (!isCurrentValid)
                {
                    return BadRequest(new { success = false, message = "Mật khẩu hiện tại không đúng." });
                }

                // Hash mật khẩu mới và lưu
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                var updated = await _userService.UpdateUserAsync(user);

                if (!updated)
                {
                    return StatusCode(500, new { success = false, message = "Không thể cập nhật mật khẩu, vui lòng thử lại." });
                }

                _logger.LogInformation("Đổi mật khẩu thành công cho user ID: {Id}", id);
                return Ok(new { success = true, message = "Đổi mật khẩu thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đổi mật khẩu cho user ID: {Id}", id);
                return StatusCode(500, new { success = false, message = "Lỗi máy chủ, vui lòng thử lại sau." });
            }
        }
    }

    // -------------------------------------------------------
    // Request DTOs (chỉ dùng nội bộ trong controller này)
    // -------------------------------------------------------
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword    { get; set; } = string.Empty;
    }
}
