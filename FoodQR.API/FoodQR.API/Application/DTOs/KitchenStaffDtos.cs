namespace FoodQR.API.Application.DTOs
{
    public class KitchenItemDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = null!;
        public string? TableNumber { get; set; }
        public string DishName { get; set; } = null!;
        public int Quantity { get; set; }
        public string? Note { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

    public class OrderDetailDto
    {
        public int Id { get; set; }
        public string OrderCode { get; set; } = null!;
        public string? TableNumber { get; set; }
        public string? Status { get; set; }
        public string? PaymentStatus { get; set; }
        public decimal TotalAmount { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public int? CouponId { get; set; }
        public string? CouponCode { get; set; }
        public decimal? DiscountAmount { get; set; }
        public List<OrderItemDetailDto> Items { get; set; } = new();
    }
}
