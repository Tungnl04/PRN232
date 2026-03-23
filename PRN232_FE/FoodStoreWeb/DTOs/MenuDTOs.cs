
namespace FoodStoreWeb.DTOs
{
    public class CategoryDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<ProductDto> Products { get; set; } = new();
    }

    public class ProductDto
    {
        public decimal Id { get; set; }
        public decimal? CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public decimal? Inventory { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
    }

    public class ComboDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public bool? Available { get; set; }
        public List<ComboItemDto> Comboitems { get; set; } = new();
    }

    public class ComboItemDto
    {
        public decimal Id { get; set; }
        public decimal? Quantity { get; set; }
        public ProductDto? Product { get; set; }
    }
}
