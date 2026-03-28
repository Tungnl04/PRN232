using System.ComponentModel.DataAnnotations;

namespace FoodQR.API.Application.DTOs
{
    public class LoginDto 
    { 
        [Required(ErrorMessage = "Tài khoản không được để trống")]
        public string Username { get; set; } = null!; 
        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        public string Password { get; set; } = null!; 
    }
}
