using FoodQR.API.Core.Entities;
using FoodQR.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodQR.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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

            order.PaymentMethod = method;
            string oldStatus = order.Status ?? "unknown";
            string oldPaymentStatus = order.PaymentStatus ?? "pending";

            if (simulateSuccess)
            {
                order.PaymentStatus = "success";
                order.Status = "paid";
                order.UpdatedAt = DateTime.Now;

                // Business Rule: Table becomes available
                if (order.Table != null)
                {
                    order.Table.Status = "available";
                }

                await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory {
                    Order = order,
                    OldStatus = oldStatus,
                    NewStatus = "paid",
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
