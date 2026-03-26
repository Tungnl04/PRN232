using Microsoft.AspNetCore.Http;

namespace FoodQR.API.Core.Interfaces
{
    public interface IPaymentService
    {
        Task<(bool Success, string? Error, object? Result)> ProcessPaymentAsync(int orderId, string method, bool simulateSuccess = true);
        Task<(bool Success, string? Error)> ExpirePaymentAsync(int orderId);

        /// <summary>
        /// Tạo VNPay payment URL cho đơn hàng
        /// </summary>
        Task<(bool Success, string? Error, string? PaymentUrl)> CreateVnPayUrlAsync(int orderId, HttpContext httpContext);

        /// <summary>
        /// Xử lý VNPay return callback (verify signature + cập nhật đơn hàng)
        /// </summary>
        Task<(bool Success, string? Error, string? OrderCode)> VnPayReturnAsync(IQueryCollection query);
    }
}
