using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using FoodStoreWeb.Services;
using FoodStoreWeb.DTOs;
using FoodStoreWeb.Interfaces;

namespace FoodStoreWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly IApiClient _apiClient;

        public AccountController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        // ViewModels

        public class LoginViewModel
        {
            [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
            public string Username { get; set; } = string.Empty;

            [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            public bool RememberMe { get; set; }
        }

        public class ProfileViewModel
        {
            public decimal Id { get; set; }

            [Required(ErrorMessage = "Tên là bắt buộc")]
            [Display(Name = "Họ và tên")]
            public string Name { get; set; } = string.Empty;

            [Display(Name = "Tên đăng nhập")]
            public string Username { get; set; } = string.Empty;

            [Display(Name = "Vai trò")]
            public string? Role { get; set; }
        }

        public class ChangePasswordViewModel
        {
            [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc")]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu hiện tại")]
            public string CurrentPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu mới")]
            [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
            public string NewPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
            [DataType(DataType.Password)]
            [Display(Name = "Xác nhận mật khẩu mới")]
            [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        // Login 

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var response = await _apiClient.LoginAsync(new LoginRequestDto
            {
                Username = model.Username,
                Password = model.Password
            });

            if (!response.Success || response.User == null)
            {
                // API trả về message cụ thể
                ModelState.AddModelError("", response.Message ?? "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View(model);
            }

            var user = response.User;
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim("Username", user.Username),
                new Claim(ClaimTypes.Role, user.Role ?? "user")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(30)
                    : DateTimeOffset.UtcNow.AddHours(24)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return RedirectToAction("Index", "Home");
        }

        // Logout 

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["InfoMessage"] = "Bạn đã đăng xuất thành công.";
            return RedirectToAction("Login", "Account");
        }

        // Profile

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (!User.Identity!.IsAuthenticated)
                return RedirectToAction("Login");

            var userId = GetCurrentUserId();
            var user = await _apiClient.GetUserByIdAsync(userId);

            if (user == null) return NotFound();

            return View(new ProfileViewModel
            {
                Id = user.Id,
                Name = user.Name,
                Username = user.Username,
                Role = user.Role
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!User.Identity!.IsAuthenticated)
                return RedirectToAction("Login");

            if (!ModelState.IsValid)
                return View(model);

            var userId = GetCurrentUserId();
            var success = await _apiClient.UpdateUserAsync(userId, new UpdateUserDto { Name = model.Name });

            if (success)
            {
                // Cập nhật lại cookie claim Name
                await RefreshNameClaimAsync(model.Name);
                TempData["SuccessMessage"] = "Cập nhật thông tin thành công.";
                return RedirectToAction("Profile");
            }

            ModelState.AddModelError("", "Không thể cập nhật thông tin. Vui lòng thử lại.");
            return View(model);
        }

        // Change Password 

        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (!User.Identity!.IsAuthenticated)
                return RedirectToAction("Login");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!User.Identity!.IsAuthenticated)
                return RedirectToAction("Login");

            if (!ModelState.IsValid)
                return View(model);

            var userId = GetCurrentUserId();
            var success = await _apiClient.ChangePasswordAsync(userId, new ChangePasswordDto
            {
                CurrentPassword = model.CurrentPassword,
                NewPassword = model.NewPassword
            });

            if (success)
            {
                TempData["SuccessMessage"] = "Đổi mật khẩu thành công.";
                return RedirectToAction("Profile");
            }

            ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng hoặc không thể đổi mật khẩu.");
            return View(model);
        }

        // Helpers 

        private decimal GetCurrentUserId()
            => decimal.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        private async Task RefreshNameClaimAsync(string newName)
        {
            var userId = GetCurrentUserId();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, newName),
                new Claim("Username", User.FindFirst("Username")?.Value ?? ""),
                new Claim(ClaimTypes.Role, User.FindFirst(ClaimTypes.Role)?.Value ?? "user")
            };

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
        }
    }
}