using System.ComponentModel.DataAnnotations;

namespace FoodQR.API.Application.DTOs
{
    public class ComboCreateDto
    {
        [Required(ErrorMessage = "Tên Combo không được để trống")]
        public string Name { get; set; } = null!;
        
        public string? Description { get; set; }
        
        [Range(0, 50000000, ErrorMessage = "Giá phải từ 0 đến 50 triệu")]
        public decimal Price { get; set; }
        
        public string? ImageUrl { get; set; }
        
        public bool Available { get; set; } = true;

        public List<ComboItemCreateDto> Items { get; set; } = new();
    }

    public class ComboItemCreateDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }
}
