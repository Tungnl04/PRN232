using FoodQR.API.Application.DTOs;
using FoodQR.API.Core.Entities;
using FoodQR.API.Infrastructure.Persistence;
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

        [HttpGet("items")]
        public async Task<ActionResult<IEnumerable<KitchenItemDto>>> GetKitchenItems()
        {
            var items = await _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Combo)
                .Include(oi => oi.Order)
                    .ThenInclude(o => o.Table)
                .Where(oi => oi.Status == "pending" || oi.Status == "preparing" || oi.Status == "ready")
                .OrderBy(oi => oi.Order.CreatedAt)
                .ToListAsync();

            return items.Select(oi => new KitchenItemDto
            {
                Id = oi.Id,
                OrderId = oi.OrderId,
                OrderCode = oi.Order.OrderCode,
                TableNumber = oi.Order.Table?.TableNumber,
                DishName = oi.Product?.Name ?? oi.Combo?.Name ?? "Unknown",
                Quantity = oi.Quantity ?? 1,
                Note = oi.Note,
                Status = oi.Status ?? "pending",
                CreatedAt = oi.Order.CreatedAt ?? DateTime.Now
            }).ToList();
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

            // Business Rule: State Machine Logic
            var order = item.Order;
            if (order != null)
            {
                string oldOrderStatus = order.Status ?? "pending";

                // 1. Chuyển sang PROCESSING nếu có món đang nấu hoặc đã xong một phần
                bool anyWorkStarted = order.OrderItems.Any(oi => oi.Status == "preparing" || oi.Status == "ready");
                if (anyWorkStarted && string.Equals(order.Status, "pending", StringComparison.OrdinalIgnoreCase))
                {
                    order.Status = "processing";
                    order.UpdatedAt = DateTime.Now;
                    await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory {
                        Order = order,
                        OldStatus = oldOrderStatus,
                        NewStatus = "processing",
                        Note = "Hệ thống: Bếp đã bắt đầu chế biến ít nhất 1 món"
                    });
                }

                // 2. Chuyển sang READY nếu TẤT CẢ các món đã xong (hoặc bị hủy)
                bool allDone = order.OrderItems.All(oi => oi.Status == "ready" || oi.Status == "served" || oi.Status == "cancelled");
                if (allDone && !string.Equals(order.Status, "ready", StringComparison.OrdinalIgnoreCase) 
                            && !string.Equals(order.Status, "served", StringComparison.OrdinalIgnoreCase))
                {
                    order.Status = "ready";
                    order.UpdatedAt = DateTime.Now;
                    await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory {
                        Order = order,
                        OldStatus = order.Status == "ready" ? oldOrderStatus : order.Status,
                        NewStatus = "ready",
                        Note = "Hệ thống: Tất cả món đã sẵn sàng"
                    });

                    await _context.Notifications.AddAsync(new Notification {
                        Message = $"Bàn {order.TableId}: Đơn {order.OrderCode} đã sẵn sàng phục vụ!",
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
