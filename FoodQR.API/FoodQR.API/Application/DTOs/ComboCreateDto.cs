namespace FoodQR.API.Application.DTOs
{
    public class ComboCreateDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
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
