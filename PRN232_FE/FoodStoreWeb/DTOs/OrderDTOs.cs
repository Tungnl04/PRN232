namespace FoodStoreWeb.DTOs
{
    public class OrderDto
    {
        public decimal Id { get; set; }
        public string? OrderCode { get; set; }
        public string? CustomerName { get; set; }
        public string? Status { get; set; }
        public decimal? TotalAmount { get; set; }
        public DateTime? CreatedAt { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderItemDto
    {
        public decimal Id { get; set; }
        public string? ProductName { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? Price { get; set; }
        public string? Type { get; set; }
    }

    public class CreateOrderDto
    {
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
        public List<CreateOrderItemDto> Items { get; set; } = new();
    }

    public class CreateOrderItemDto
    {
        public decimal ProductId { get; set; }
        public decimal Quantity { get; set; }
        public string Type { get; set; } = "product";
    }

    public class CreateOrderResponseDto
    {
        public bool Success { get; set; }
        public string? OrderCode { get; set; }
        public string? Message { get; set; }
    }

    public class OrdersByCodeResponseDto
    {
        public bool Success { get; set; }
        public string? CustomerName { get; set; }
        public List<OrderDto>? Orders { get; set; }
        public string? Message { get; set; }
    }
}