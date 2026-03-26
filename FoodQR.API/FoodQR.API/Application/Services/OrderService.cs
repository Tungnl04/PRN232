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
    public class OrderService : IOrderService
    {
        private readonly FoodStoreDbContext _context;
        private readonly IHubContext<OrderHub> _hubContext;

        public OrderService(FoodStoreDbContext context, IHubContext<OrderHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<Order> CreateOrAppendOrderAsync(OrderCreateDto orderDto)
        {
            // Validate Token
            var table = await _context.OrderTables.FindAsync(orderDto.TableId);
            if (table == null)
            {
                throw new ArgumentException("Bàn không tồn tại.");
            }
            if (string.IsNullOrEmpty(orderDto.Token) || table.QrCodeToken != orderDto.Token)
            {
                throw new UnauthorizedAccessException("QR Token không hợp lệ hoặc đã hết hạn. Vui lòng quét lại mã QR tại bàn.");
            }

            // 1. Check for active order on this table
            var activeOrder = await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.TableId == orderDto.TableId &&
                           !new[] { OrderStatus.Paid, OrderStatus.Cancelled, OrderStatus.Rejected }.Contains(o.Status))
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            Order order;
            if (activeOrder != null)
            {
                order = activeOrder;

                // Business Rule: Nếu đơn đang Ready/Served mà gọi thêm → đưa về Pending
                if (string.Equals(order.Status, OrderStatus.Ready, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(order.Status, OrderStatus.Served, StringComparison.OrdinalIgnoreCase))
                {
                    string oldStatus = order.Status!;
                    order.Status = OrderStatus.Pending;
                    order.UpdatedAt = DateTime.Now;

                    await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
                    {
                        Order = order,
                        OldStatus = oldStatus,
                        NewStatus = OrderStatus.Pending,
                        Note = "Khách đặt thêm món mới"
                    });
                }

                await _context.ActivityLogs.AddAsync(new ActivityLog
                {
                    Action = "append_items",
                    Description = $"Guest appended items to Order {order.OrderCode} at Table {orderDto.TableId}"
                });
            }
            else
            {
                // Create new customer
                var customer = new Customer
                {
                    Name = orderDto.CustomerName ?? "Khách",
                    Email = orderDto.CustomerEmail
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                order = new Order
                {
                    OrderCode = "ORD-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
                    CustomerId = customer.Id,
                    TableId = orderDto.TableId,
                    Status = OrderStatus.Pending,
                    PaymentStatus = PaymentStatus.Pending,
                    CreatedAt = DateTime.Now
                };
                _context.Orders.Add(order);

                await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
                {
                    Order = order,
                    NewStatus = OrderStatus.Pending,
                    Note = "Order initiated"
                });

                await _context.ActivityLogs.AddAsync(new ActivityLog
                {
                    Action = "create_order",
                    Description = $"New order {order.OrderCode} created at Table {orderDto.TableId}"
                });

                await _context.Notifications.AddAsync(new Notification
                {
                    Message = $"🔔 Bàn {orderDto.TableId} vừa đặt món mới! Đơn: {order.OrderCode}",
                    Type = "new_order",
                    TargetRole = AppRoles.Staff,
                    CreatedAt = DateTime.Now
                });
            }

            // 2. Process items + inventory
            decimal additionalAmount = 0;
            foreach (var itemDto in orderDto.Items)
            {
                // Normalize: 0 → null (tránh FK constraint error)
                if (itemDto.ProductId == 0) itemDto.ProductId = null;
                if (itemDto.ComboId == 0) itemDto.ComboId = null;

                if (!itemDto.ProductId.HasValue && !itemDto.ComboId.HasValue) continue;

                decimal unitPrice = 0;
                if (itemDto.ProductId.HasValue)
                {
                    var product = await _context.Products.FindAsync(itemDto.ProductId.Value);
                    if (product == null || product.IsAvailable == false) continue;

                    // Inventory check & deduct
                    if (product.Inventory.HasValue && product.Inventory.Value < itemDto.Quantity)
                        continue; // Skip if not enough stock

                    if (product.Inventory.HasValue)
                    {
                        product.Inventory -= itemDto.Quantity;
                        // Auto-disable when out of stock
                        if (product.Inventory <= 0)
                        {
                            product.IsAvailable = false;
                            await _context.Notifications.AddAsync(new Notification
                            {
                                Message = $"⚠️ Sản phẩm '{product.Name}' đã hết hàng!",
                                Type = "low_inventory",
                                TargetRole = AppRoles.Admin,
                                CreatedAt = DateTime.Now
                            });
                        }
                    }
                    unitPrice = product.Price;
                }
                else if (itemDto.ComboId.HasValue)
                {
                    var combo = await _context.Combos
                        .Include(c => c.ComboItems)
                        .ThenInclude(ci => ci.Product)
                        .FirstOrDefaultAsync(c => c.Id == itemDto.ComboId.Value);

                    if (combo == null || combo.Available == false) continue;

                    // Deduct inventory for child products
                    bool canFulfillCombo = true;
                    foreach (var comboItem in combo.ComboItems)
                    {
                        var product = comboItem.Product;
                        int requiredQty = (comboItem.Quantity ?? 1) * itemDto.Quantity;
                        
                        if (product.Inventory.HasValue && product.Inventory.Value < requiredQty)
                        {
                            canFulfillCombo = false;
                            break;
                        }
                    }

                    if (!canFulfillCombo) continue; // Skip if any product in combo is out of stock

                    foreach (var comboItem in combo.ComboItems)
                    {
                        var product = comboItem.Product;
                        int requiredQty = (comboItem.Quantity ?? 1) * itemDto.Quantity;

                        if (product.Inventory.HasValue)
                        {
                            product.Inventory -= requiredQty;
                            if (product.Inventory <= 0)
                            {
                                product.IsAvailable = false;
                                await _context.Notifications.AddAsync(new Notification
                                {
                                    Message = $"⚠️ Sản phẩm '{product.Name}' (combo '{combo.Name}') đã hết hàng!",
                                    Type = "low_inventory",
                                    TargetRole = AppRoles.Admin,
                                    CreatedAt = DateTime.Now
                                });
                            }
                        }
                    }

                    unitPrice = combo.Price;
                }

                var orderItem = new OrderItem
                {
                    ProductId = itemDto.ProductId,
                    ComboId = itemDto.ComboId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = unitPrice,
                    Status = OrderItemStatus.Pending,
                    Note = itemDto.Note
                };
                order.OrderItems.Add(orderItem);
                additionalAmount += unitPrice * itemDto.Quantity;
            }

            order.TotalAmount = (order.TotalAmount ?? 0) + additionalAmount;

            // 3. Update table status
            table.Status = TableStatus.Taken;

            await _context.SaveChangesAsync();

            // === SignalR Broadcast ===
            var orderPayload = new { orderId = order.Id, orderCode = order.OrderCode, tableId = orderDto.TableId };
            await _hubContext.Clients.Group("kitchen").SendAsync("NewOrderReceived", orderPayload);
            await _hubContext.Clients.Group("staff").SendAsync("NewOrderReceived", orderPayload);

            return order;
        }

        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<OrderDetailDto?> GetActiveOrderByTableAsync(int tableId)
        {
            var order = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Combo)
                .Where(o => o.TableId == tableId &&
                           !new[] { OrderStatus.Paid, OrderStatus.Cancelled, OrderStatus.Rejected }.Contains(o.Status))
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (order == null) return null;

            return new OrderDetailDto
            {
                Id = order.Id,
                OrderCode = order.OrderCode,
                TableNumber = order.Table?.TableNumber,
                Status = order.Status,
                PaymentStatus = order.PaymentStatus,
                TotalAmount = order.TotalAmount ?? 0,
                Items = order.OrderItems.Select(oi => new OrderItemDetailDto
                {
                    Id = oi.Id,
                    Name = oi.Product?.Name ?? oi.Combo?.Name ?? "Unknown",
                    Quantity = oi.Quantity ?? 1,
                    UnitPrice = oi.UnitPrice,
                    Status = oi.Status ?? OrderItemStatus.Pending,
                    Note = oi.Note
                }).ToList()
            };
        }

        public async Task<object> GetDashboardStatsAsync()
        {
            var totalOrders = await _context.Orders.CountAsync();
            var totalRevenue = await _context.Orders
                .Where(o => o.Status == OrderStatus.Paid)
                .SumAsync(o => o.TotalAmount ?? 0);
            var activeTables = await _context.OrderTables
                .CountAsync(t => t.Status == TableStatus.Taken);

            return new { totalOrders, totalRevenue, activeTables };
        }

        public async Task<bool> CancelOrderAsync(int orderId, string? reason)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return false;

            // Only cancel pending/processing orders
            var cancellableStatuses = new[] { OrderStatus.Pending, OrderStatus.Processing };
            if (!cancellableStatuses.Contains(order.Status?.ToLower()))
                return false;

            string oldStatus = order.Status!;
            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.Now;

            // Restore inventory for all items
            foreach (var item in order.OrderItems.Where(i => i.Status != OrderItemStatus.Cancelled))
            {
                item.Status = OrderItemStatus.Cancelled;
                if (item.Product != null && item.Quantity.HasValue)
                {
                    item.Product.Inventory = (item.Product.Inventory ?? 0) + item.Quantity.Value;
                    if (item.Product.Inventory > 0) item.Product.IsAvailable = true;
                }
            }

            // Free up table
            if (order.Table != null) order.Table.Status = TableStatus.Available;

            await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
            {
                Order = order, OldStatus = oldStatus, NewStatus = OrderStatus.Cancelled,
                Note = reason ?? "Đơn hàng bị hủy"
            });

            await _context.ActivityLogs.AddAsync(new ActivityLog
            {
                Action = "cancel_order",
                Description = $"Order {order.OrderCode} cancelled. Reason: {reason ?? "N/A"}"
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelOrderItemAsync(int orderItemId, string? reason)
        {
            var item = await _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Order)
                .FirstOrDefaultAsync(oi => oi.Id == orderItemId);

            if (item == null) return false;
            if (item.Status != OrderItemStatus.Pending) return false;

            item.Status = OrderItemStatus.Cancelled;
            item.RejectionReason = reason;

            // Restore inventory
            if (item.Product != null && item.Quantity.HasValue)
            {
                item.Product.Inventory = (item.Product.Inventory ?? 0) + item.Quantity.Value;
                if (item.Product.Inventory > 0) item.Product.IsAvailable = true;
            }

            // Recalculate order total
            if (item.Order != null)
            {
                var remainingTotal = await _context.OrderItems
                    .Where(oi => oi.OrderId == item.OrderId && oi.Status != OrderItemStatus.Cancelled)
                    .SumAsync(oi => oi.UnitPrice * (oi.Quantity ?? 1));
                item.Order.TotalAmount = remainingTotal;
            }

            await _context.ActivityLogs.AddAsync(new ActivityLog
            {
                Action = "cancel_item",
                Description = $"Item {orderItemId} cancelled. Reason: {reason ?? "N/A"}"
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SwitchTableAsync(int orderId, int newTableId)
        {
            var order = await _context.Orders
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null || new[] { OrderStatus.Paid, OrderStatus.Cancelled, OrderStatus.Rejected }.Contains(order.Status))
                return false;

            if (order.TableId == newTableId)
                return false; // Already on this table

            var newTable = await _context.OrderTables.FindAsync(newTableId);
            if (newTable == null || newTable.Status == TableStatus.Taken)
                return false; // Table doesn't exist or is currently occupied

            var oldTable = order.Table;
            
            // Switch
            order.TableId = newTableId;
            newTable.Status = TableStatus.Taken;

            if (oldTable != null)
            {
                // Check if old table has any other active orders (edge case), if not set to Available
                bool hasOtherActiveOrders = await _context.Orders.AnyAsync(o => 
                    o.TableId == oldTable.Id && o.Id != order.Id && 
                    !new[] { OrderStatus.Paid, OrderStatus.Cancelled, OrderStatus.Rejected }.Contains(o.Status));
                
                if (!hasOtherActiveOrders)
                {
                    oldTable.Status = TableStatus.Available;
                }
            }

            await _context.ActivityLogs.AddAsync(new ActivityLog
            {
                Action = "switch_table",
                Description = $"Order {order.OrderCode} switched from Table {(oldTable != null ? oldTable.TableNumber : "None")} to Table {newTable.TableNumber}."
            });

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
