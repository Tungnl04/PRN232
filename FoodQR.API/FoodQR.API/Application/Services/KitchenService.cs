using FoodQR.API.Application.DTOs;
using FoodQR.API.Core.Entities;
using FoodQR.API.Core.Enums;
using FoodQR.API.Core.Interfaces;
using FoodQR.API.Hubs;
using FoodQR.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FoodQR.API.Application.Services
{
    public class KitchenService : IKitchenService
    {
        private readonly FoodStoreDbContext _context;
        private readonly IHubContext<OrderHub> _hubContext;

        public KitchenService(FoodStoreDbContext context, IHubContext<OrderHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<List<KitchenItemDto>> GetKitchenItemsAsync()
        {
            return await _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Combo)
                .Include(oi => oi.Order)
                .Where(oi => oi.Status != OrderItemStatus.Served && oi.Status != OrderItemStatus.Cancelled)
                .OrderBy(oi => oi.Order!.CreatedAt)
                .Select(oi => new KitchenItemDto
                {
                    Id = oi.Id,
                    OrderId = oi.OrderId,
                    DishName = oi.Product != null ? oi.Product.Name : (oi.Combo != null ? oi.Combo.Name : "Unknown"),
                    Quantity = oi.Quantity ?? 1,
                    Status = oi.Status ?? OrderItemStatus.Pending,
                    OrderCode = oi.Order!.OrderCode,
                    TableNumber = oi.Order.Table != null ? oi.Order.Table.TableNumber : null,
                    Note = oi.Note,
                    CreatedAt = oi.Order.CreatedAt ?? DateTime.Now
                })
                .ToListAsync();
        }

        public async Task<bool> UpdateItemStatusAsync(int itemId, string newStatus)
        {
            var validStatuses = new[] { OrderItemStatus.Preparing, OrderItemStatus.Ready, OrderItemStatus.Served, OrderItemStatus.Cancelled };
            if (!validStatuses.Contains(newStatus.ToLower()))
                return false;

            var item = await _context.OrderItems
                .Include(oi => oi.Order).ThenInclude(o => o!.OrderItems)
                .Include(oi => oi.Order).ThenInclude(o => o!.Table)
                .Include(oi => oi.Product)
                .FirstOrDefaultAsync(oi => oi.Id == itemId);

            if (item == null) return false;

            // Validate transitions
            if (newStatus == OrderItemStatus.Cancelled && item.Status != OrderItemStatus.Pending)
                return false;

            string oldItemStatus = item.Status ?? OrderItemStatus.Pending;
            item.Status = newStatus;

            // Restore inventory on cancel
            if (newStatus == OrderItemStatus.Cancelled && item.Product != null && item.Quantity.HasValue)
            {
                item.Product.Inventory = (item.Product.Inventory ?? 0) + item.Quantity.Value;
                if (item.Product.Inventory > 0) item.Product.IsAvailable = true;
            }

            await _context.ActivityLogs.AddAsync(new ActivityLog
            {
                Action = "update_item_status",
                Description = $"Item {itemId} status changed from {oldItemStatus} to {newStatus}"
            });

            // State machine: Update parent order status
            var order = item.Order;
            bool justStartedProcessing = false;
            if (order != null)
            {
                string oldOrderStatus = order.Status ?? OrderStatus.Pending;

                // → PROCESSING if any work started (first time only)
                bool anyWorkStarted = order.OrderItems.Any(oi => oi.Status == OrderItemStatus.Preparing || oi.Status == OrderItemStatus.Ready);
                bool wasJustPending = string.Equals(oldOrderStatus, OrderStatus.Pending, StringComparison.OrdinalIgnoreCase);
                if (anyWorkStarted && wasJustPending)
                {
                    order.Status = OrderStatus.Processing;
                    order.UpdatedAt = DateTime.Now;
                    justStartedProcessing = true; // Flag for SignalR below
                    await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
                    {
                        Order = order, OldStatus = oldOrderStatus, NewStatus = OrderStatus.Processing,
                        Note = "Bếp đã bắt đầu chế biến"
                    });
                }

                // → READY if all done (but not yet served)
                bool allDoneReady = order.OrderItems.All(oi =>
                    oi.Status == OrderItemStatus.Ready || oi.Status == OrderItemStatus.Served || oi.Status == OrderItemStatus.Cancelled);
                if (allDoneReady && order.Status != OrderStatus.Ready && order.Status != OrderStatus.Served)
                {
                    string statusBeforeReady = order.Status ?? oldOrderStatus;
                    order.Status = OrderStatus.Ready;
                    order.UpdatedAt = DateTime.Now;
                    await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
                    {
                        Order = order, OldStatus = statusBeforeReady, NewStatus = OrderStatus.Ready,
                        Note = "Tất cả món đã sẵn sàng"
                    });

                    await _context.Notifications.AddAsync(new Notification
                    {
                        Message = $"Bàn {order.TableId}: Đơn {order.OrderCode} đã sẵn sàng phục vụ!",
                        Type = "order_ready",
                        TargetRole = AppRoles.Staff,
                        CreatedAt = DateTime.Now
                    });
                }

                // → SERVED if all items are served or cancelled (and at least some were served)
                bool allServed = order.OrderItems.All(oi => oi.Status == OrderItemStatus.Served || oi.Status == OrderItemStatus.Cancelled) 
                                 && order.OrderItems.Any(oi => oi.Status == OrderItemStatus.Served);
                if (allServed && order.Status != OrderStatus.Served)
                {
                    string statusBeforeServed = order.Status ?? oldOrderStatus;
                    order.Status = OrderStatus.Served;
                    order.UpdatedAt = DateTime.Now;
                    await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
                    {
                        Order = order, OldStatus = statusBeforeServed, NewStatus = OrderStatus.Served,
                        Note = "Tất cả món đã được phục vụ"
                    });
                }
            }

            await _context.SaveChangesAsync();

            // === SignalR Broadcast ===
            var tableId = order?.TableId;
            var payload = new { itemId, newStatus, orderId = order?.Id, orderStatus = order?.Status, tableId };

            // Kitchen & Staff always get per-item updates (for board refresh)
            await _hubContext.Clients.Group("kitchen").SendAsync("ItemStatusChanged", payload);
            await _hubContext.Clients.Group("staff").SendAsync("ItemStatusChanged", payload);

            // Customer chỉ nhận thông báo khi ORDER chuyển trạng thái, KHÔNG phải từng món:
            // 1) Đơn BẮT ĐẦU chế biến (lần đầu tiên)
            if (order != null && justStartedProcessing && tableId.HasValue)
            {
                var startPayload = new { orderId = order.Id, orderCode = order.OrderCode, tableId };
                await _hubContext.Clients.Group($"table_{tableId}").SendAsync("OrderStartedPreparing", startPayload);
            }

            // 2) Đơn HOÀN TẤT tất cả món
            if (order != null && order.Status == OrderStatus.Ready)
            {
                var readyPayload = new { orderId = order.Id, orderCode = order.OrderCode, tableId };
                await _hubContext.Clients.Group("staff").SendAsync("OrderReady", readyPayload);
                if (tableId.HasValue)
                    await _hubContext.Clients.Group($"table_{tableId}").SendAsync("OrderReady", readyPayload);
            }

            return true;
        }
    }
}
