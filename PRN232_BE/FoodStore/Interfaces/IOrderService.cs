using FoodStoreAPI.DTOs;
using static FoodStoreAPI.DTOs.FindOrderDTO;


namespace FoodStoreAPI.Interfaces
{
    public interface IOrderService
    {
        Task<CreateOrderResult> CreateOrderAsync(CreateOrderDTO request);
        Task<GetOrdersByOrderCodeResult> GetOrdersByOrderCodeAsync(string OrderCode);
    }

    public class CreateOrderResult
    {
        public bool Success { get; set; }
        public string? OrderCode { get; set; }
        public string? Message { get; set; }
    }
}
