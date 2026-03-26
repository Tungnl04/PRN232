namespace FoodQR.API.Application.DTOs
{
    public class ProductResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public int? Inventory { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public bool? IsAvailable { get; set; }
    }

    public class TableResponseDto
    {
        public int Id { get; set; }
        public string TableNumber { get; set; } = null!;
        public int Capacity { get; set; }
        public string? Status { get; set; }
        public string? QrCodeToken { get; set; }
        public string? Location { get; set; }
    }

    public class UserResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string? Role { get; set; }
        public bool? Active { get; set; }
        public bool MustChangePassword { get; set; }
    }
}
