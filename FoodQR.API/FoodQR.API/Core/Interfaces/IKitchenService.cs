using FoodQR.API.Application.DTOs;

namespace FoodQR.API.Core.Interfaces
{
    public interface IKitchenService
    {
        Task<List<KitchenItemDto>> GetKitchenItemsAsync();
        Task<bool> UpdateItemStatusAsync(int itemId, string newStatus);
    }
}
