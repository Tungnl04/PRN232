using FoodQR.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodQR.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KitchenController : ControllerBase
    {
        private readonly FoodStoreDbContext _context;

        public KitchenController(FoodStoreDbContext context)
        {
            _context = context;
        }

        [HttpGet("items/pending")]
        public async Task<ActionResult<IEnumerable<OrderItem>>> GetPendingItems()
        {
            return await _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Combo)
                .Include(oi => oi.Order)
                    .ThenInclude(o => o.Table)
                .Where(oi => oi.Status == "pending" || oi.Status == "preparing")
                .OrderBy(oi => oi.Order.CreatedAt)
                .ToListAsync();
        }

        [HttpPatch("items/{id}/status")]
        public async Task<IActionResult> UpdateItemStatus(int id, [FromBody] string status)
        {
            var item = await _context.OrderItems
                .Include(oi => oi.Order)
                .ThenInclude(o => o.OrderItems)
                .FirstOrDefaultAsync(oi => oi.Id == id);

            if (item == null) return NotFound();

            // Business Rule: Chỉ hủy item khi pending
            if (status == "cancelled" && item.Status != "pending")
            {
                return BadRequest("Only pending items can be cancelled.");
            }

            string oldStatus = item.Status ?? "unknown";
            item.Status = status;

            // Log activity
            await _context.ActivityLogs.AddAsync(new ActivityLog {
                Action = "update_item_status",
                Description = $"Item {id} status changed from {oldStatus} to {status}"
            });

            // Business Rule: Khi tất cả item ready thì order tự ready
            var order = item.Order;
            if (order != null)
            {
                bool allReady = order.OrderItems.All(oi => oi.Status == "ready" || oi.Status == "served" || oi.Status == "cancelled");
                if (allReady && order.Status != "ready")
                {
                    string oldOrderStatus = order.Status ?? "unknown";
                    order.Status = "ready";
                    order.UpdatedAt = DateTime.Now;

                    await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory {
                        Order = order,
                        OldStatus = oldOrderStatus,
                        NewStatus = "ready",
                        Note = "System: All items are ready"
                    });

                    await _context.Notifications.AddAsync(new Notification {
                        Message = $"Đơn hàng {order.OrderCode} đã sẵn sàng toàn bộ!",
                        Type = "order_ready",
                        TargetRole = "staff",
                        CreatedAt = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
