namespace FoodQR.API.DTOs
{
    public class CreateOrderDto
    {
        public string CustomerName { get; set; } = null!;
        public string? CustomerEmail { get; set; }
        public int TableId { get; set; }
        public List<CreateOrderItemDto> Items { get; set; } = new();
    }

    public class CreateOrderItemDto
    {
        public int? ProductId { get; set; }
        public int? ComboId { get; set; }
        public int Quantity { get; set; }
        public string? Note { get; set; }
    }
}
