using FoodQR.API.Core.Entities;
using FoodQR.API.Core.Enums;
using FoodQR.API.Core.Interfaces;
using FoodQR.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodQR.API.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly FoodStoreDbContext _context;

        public PaymentService(FoodStoreDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string? Error, object? Result)> ProcessPaymentAsync(int orderId, string method, bool simulateSuccess = true)
        {
            var order = await _context.Orders
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return (false, "Không tìm thấy đơn hàng.", null);

            // Validate order status
            var allowedStatuses = new[] { OrderStatus.Ready, OrderStatus.Served };
            if (!allowedStatuses.Contains(order.Status?.ToLower()))
                return (false, $"Chỉ có thể thanh toán đơn đã ready hoặc served. Trạng thái hiện tại: {order.Status}", null);

            if (order.PaymentStatus == PaymentStatus.Success)
                return (false, "Đơn hàng này đã được thanh toán rồi.", null);

            if (order.PaymentStatus == PaymentStatus.Expired)
                return (false, "Phiên thanh toán đã hết hạn. Vui lòng tạo phiên mới.", null);

            order.PaymentMethod = method;
            string oldStatus = order.Status ?? OrderStatus.Pending;

            if (simulateSuccess)
            {
                order.PaymentStatus = PaymentStatus.Success;
                order.Status = OrderStatus.Paid;
                order.UpdatedAt = DateTime.Now;

                if (order.Table != null)
                    order.Table.Status = TableStatus.Cleaning;

                await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
                {
                    Order = order, OldStatus = oldStatus, NewStatus = OrderStatus.Paid,
                    Note = $"Payment success via {method}"
                });

                await _context.ActivityLogs.AddAsync(new ActivityLog
                {
                    Action = "payment_success",
                    Description = $"Order {order.OrderCode} paid via {method}"
                });

                await _context.Notifications.AddAsync(new Notification
                {
                    Message = $"Đơn {order.OrderCode} đã thanh toán thành công!",
                    Type = "payment_success",
                    TargetRole = AppRoles.Admin,
                    CreatedAt = DateTime.Now
                });
            }
            else
            {
                order.PaymentStatus = PaymentStatus.Failed;
                await _context.ActivityLogs.AddAsync(new ActivityLog
                {
                    Action = "payment_failed",
                    Description = $"Order {order.OrderCode} payment failed via {method}"
                });
            }

            await _context.SaveChangesAsync();
            return (true, null, new { order.Status, order.PaymentStatus });
        }

        public async Task<(bool Success, string? Error)> ExpirePaymentAsync(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return (false, "Không tìm thấy đơn hàng.");

            if (order.PaymentStatus != PaymentStatus.Pending && order.PaymentStatus != PaymentStatus.Failed)
                return (false, $"Chỉ hết hạn thanh toán pending/failed. Hiện tại: {order.PaymentStatus}");

            order.PaymentStatus = PaymentStatus.Expired;
            await _context.ActivityLogs.AddAsync(new ActivityLog
            {
                Action = "payment_expired",
                Description = $"Payment for order {order.OrderCode} expired"
            });

            await _context.SaveChangesAsync();
            return (true, null);
        }
    }
}
