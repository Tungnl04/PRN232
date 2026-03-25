using FoodQR.API.Core.Entities;
using FoodQR.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodQR.API.Core.Enums;

namespace FoodQR.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "staff,admin")]
    public class PaymentsController : ControllerBase
    {
        private readonly FoodStoreDbContext _context;

        public PaymentsController(FoodStoreDbContext context)
        {
            _context = context;
        }

        [HttpPost("{orderId}/process")]
        public async Task<IActionResult> ProcessPayment(int orderId, [FromQuery] string method, [FromQuery] bool simulateSuccess = true)
        {
            var order = await _context.Orders
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound();

            // BUG-07 Fix: Validate order status before payment
            var allowedStatuses = new[] { OrderStatus.Ready, OrderStatus.Served };
            if (!allowedStatuses.Contains(order.Status?.ToLower()))
            {
                return BadRequest(new { 
                    Error = "Chỉ có thể thanh toán đơn hàng đã sẵn sàng (ready) hoặc đã phục vụ (served).",
                    CurrentStatus = order.Status 
                });
            }

            // Prevent double payment
            if (string.Equals(order.PaymentStatus, PaymentStatus.Success, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { Error = "Đơn hàng này đã được thanh toán rồi." });
            }

            // Prevent paying expired orders
            if (string.Equals(order.PaymentStatus, PaymentStatus.Expired, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { Error = "Phiên thanh toán đã hết hạn. Vui lòng tạo phiên mới." });
            }

            order.PaymentMethod = method;
            string oldStatus = order.Status ?? "unknown";
            string oldPaymentStatus = order.PaymentStatus ?? "pending";

            if (simulateSuccess)
            {
                order.PaymentStatus = PaymentStatus.Success;
                order.Status = OrderStatus.Paid;
                order.UpdatedAt = DateTime.Now;

                // Business Rule: Table becomes available
                if (order.Table != null)
                {
                    order.Table.Status = TableStatus.Available;
                }

                await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory {
                    Order = order,
                    OldStatus = oldStatus,
                    NewStatus = OrderStatus.Paid,
                    Note = $"Payment success via {method}"
                });

                await _context.ActivityLogs.AddAsync(new ActivityLog {
                    Action = "payment_success",
                    Description = $"Order {order.OrderCode} paid via {method}"
                });

                await _context.Notifications.AddAsync(new Notification {
                    Message = $"Đơn hàng {order.OrderCode} đã được thanh toán thành công!",
                    Type = "payment_success",
                    TargetRole = "admin",
                    CreatedAt = DateTime.Now
                });
            }
            else
            {
                order.PaymentStatus = "failed";
                // Business Rule: Order remains served (or previous status) and allows retry
                await _context.ActivityLogs.AddAsync(new ActivityLog {
                    Action = "payment_failed",
                    Description = $"Order {order.OrderCode} payment failed via {method}"
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { order.Status, order.PaymentStatus });
        }

        [HttpPatch("{orderId}/expire")]
        public async Task<IActionResult> ExpirePayment(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            // Only expire pending payments
            if (!string.Equals(order.PaymentStatus, "pending", StringComparison.OrdinalIgnoreCase) 
                && !string.Equals(order.PaymentStatus, "failed", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { 
                    Error = "Chỉ có thể hết hạn thanh toán đang pending hoặc failed.",
                    CurrentPaymentStatus = order.PaymentStatus 
                });
            }

            order.PaymentStatus = "expired";
            await _context.ActivityLogs.AddAsync(new ActivityLog {
                Action = "payment_expired",
                Description = $"Payment for order {order.OrderCode} expired"
            });

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
