using FoodQR.API.Application.DTOs;
using FoodQR.API.Core.Entities;

namespace FoodQR.API.Core.Interfaces
{
    public interface IOrderService
    {
        Task<Order> CreateOrAppendOrderAsync(OrderCreateDto orderDto);
        Task<Order?> GetOrderByIdAsync(int id);
        Task<OrderDetailDto?> GetActiveOrderByTableAsync(int tableId);
        Task<object> GetDashboardStatsAsync();
        Task<bool> CancelOrderAsync(int orderId, string? reason);
        Task<bool> CancelOrderItemAsync(int orderItemId, string? reason);
    }
}
