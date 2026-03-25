namespace FoodQR.API.Application.DTOs
{
    public class OrderCreateDto
    {
        public int TableId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderItemDto
    {
        public int? ProductId { get; set; }
        public int? ComboId { get; set; }
        public int Quantity { get; set; }
        public string? Note { get; set; }
    }

    public class OrderResponseDto
    {
        public int Id { get; set; }
        public string OrderCode { get; set; } = null!;
        public string Status { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public List<OrderItemDetailDto> Items { get; set; } = new();
    }

    public class OrderItemDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string Status { get; set; } = null!;
        public string? Note { get; set; }
    }
}
