namespace FoodQR.API.Core.Interfaces
{
    public interface IPaymentService
    {
        Task<(bool Success, string? Error, object? Result)> ProcessPaymentAsync(int orderId, string method, bool simulateSuccess = true);
        Task<(bool Success, string? Error)> ExpirePaymentAsync(int orderId);
    }
}
