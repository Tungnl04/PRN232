namespace FoodQR.API.Application.DTOs
{
    public class UpdateUserDto
    {
        public string? Name { get; set; }
        public string? Role { get; set; }
        public bool? Active { get; set; }
        public string? NewPassword { get; set; }
    }
}
