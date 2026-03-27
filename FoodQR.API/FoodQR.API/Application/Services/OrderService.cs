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

        private static bool IsPercentCoupon(string? discountType)
            => string.Equals(discountType, "percent", StringComparison.OrdinalIgnoreCase);

        private static bool IsFixedCoupon(string? discountType)
            => string.Equals(discountType, "fixed", StringComparison.OrdinalIgnoreCase);

        private static decimal CalculateDiscountAmount(Coupon? coupon, decimal subtotal)
        {
            if (coupon == null) return 0m;
            if (subtotal <= 0) return 0m;
            if (subtotal < coupon.MinOrderAmount) return 0m;

            decimal discount = 0m;
            if (IsPercentCoupon(coupon.DiscountType))
            {
                var percent = coupon.DiscountValue;
                if (percent < 0) percent = 0;
                if (percent > 100) percent = 100;
                discount = subtotal * (percent / 100m);
            }
            else if (IsFixedCoupon(coupon.DiscountType))
            {
                discount = coupon.DiscountValue;
            }

            if (discount < 0) discount = 0m;
            if (discount > subtotal) discount = subtotal;
            return decimal.Round(discount, 0, MidpointRounding.AwayFromZero);
        }

        private async Task ReleaseCouponUsageAsync(int? couponId)
        {
            if (!couponId.HasValue) return;
            var coupon = await _context.Coupons.FindAsync(couponId.Value);
            if (coupon == null) return;
            if (coupon.UsedCount > 0) coupon.UsedCount--;
        }

        private async Task RecalculateOrderFinancialsAsync(Order order)
        {
            var persistedSubtotal = await _context.OrderItems
                .Where(oi => oi.OrderId == order.Id && oi.Status != OrderItemStatus.Cancelled)
                .SumAsync(oi => oi.UnitPrice * (oi.Quantity ?? 1));

            // Đồng bộ với ChangeTracker để xử lý các trường hợp:
            // - Cancel món (Status đổi sang Cancelled) nhưng chưa SaveChanges
            // - Gộp bàn (OrderId đổi) nhưng chưa SaveChanges
            // - Thêm món (Added) nhưng chưa SaveChanges
            var trackedDelta = 0m;
            foreach (var e in _context.ChangeTracker.Entries<OrderItem>())
            {
                if (e.State != EntityState.Added && e.State != EntityState.Modified && e.State != EntityState.Deleted)
                    continue;

                var currentOrderId = e.Entity.OrderId;
                var currentStatus = e.Entity.Status ?? "";
                var currentUnitPrice = e.Entity.UnitPrice;
                var currentQty = e.Entity.Quantity ?? 1;

                var originalOrderIdObj = e.Property("OrderId").OriginalValue;
                var originalOrderId = originalOrderIdObj is int oid ? oid : currentOrderId;

                var originalStatusObj = e.Property("Status").OriginalValue;
                var originalStatus = (originalStatusObj as string) ?? currentStatus;

                var originalUnitPriceObj = e.Property("UnitPrice").OriginalValue;
                var originalUnitPrice = originalUnitPriceObj is decimal oup ? oup : currentUnitPrice;

                var originalQtyObj = e.Property("Quantity").OriginalValue;
                var originalQty = originalQtyObj is int oq ? oq : (e.Entity.Quantity ?? 1);

                bool currentBelongsToOrder = currentOrderId == order.Id || ReferenceEquals(e.Entity.Order, order);
                bool originalBelongsToOrder = originalOrderId == order.Id;

                bool currentIncluded = currentBelongsToOrder &&
                    !string.Equals(currentStatus, OrderItemStatus.Cancelled, StringComparison.OrdinalIgnoreCase);
                bool originalIncluded = originalBelongsToOrder &&
                    !string.Equals(originalStatus, OrderItemStatus.Cancelled, StringComparison.OrdinalIgnoreCase);

                decimal currentAmount = currentUnitPrice * currentQty;
                decimal originalAmount = originalUnitPrice * originalQty;

                // State-aware delta:
                // Added: chưa tồn tại trong DB => chỉ cộng current
                // Deleted: đã tồn tại trong DB => chỉ trừ original
                // Modified: lấy current - original
                if (e.State == EntityState.Added)
                {
                    trackedDelta += currentIncluded ? currentAmount : 0m;
                }
                else if (e.State == EntityState.Deleted)
                {
                    trackedDelta -= originalIncluded ? originalAmount : 0m;
                }
                else
                {
                    trackedDelta += (currentIncluded ? currentAmount : 0m) - (originalIncluded ? originalAmount : 0m);
                }
            }

            // Added items are not in persistedSubtotal; Modified/Deleted adjustments are applied via trackedDelta
            // but persistedSubtotal already includes DB values, so trackedDelta corrects it to the current in-memory view.
            var subtotal = persistedSubtotal + trackedDelta;

            if (order.CouponId.HasValue)
            {
                var originalCouponId = order.CouponId;
                var coupon = order.Coupon ?? await _context.Coupons.FindAsync(order.CouponId.Value);
                var discount = CalculateDiscountAmount(coupon, subtotal);

                if (discount <= 0)
                {
                    // Nếu không còn đạt điều kiện (vd subtotal < min order), tự bỏ coupon khỏi đơn
                    order.CouponId = null;
                    order.DiscountAmount = null;
                    order.TotalAmount = subtotal;
                    await ReleaseCouponUsageAsync(originalCouponId);
                }
                else
                {
                    order.DiscountAmount = discount;
                    order.TotalAmount = subtotal - discount;
                }
            }
            else
            {
                order.DiscountAmount = null;
                order.TotalAmount = subtotal;
            }

            if (order.TotalAmount < 0) order.TotalAmount = 0;
            order.UpdatedAt = DateTime.Now;
        }

        public async Task<Order> CreateOrAppendOrderAsync(OrderCreateDto orderDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
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

                // Nếu bàn đã có coupon, không cho gửi coupon lại khi gọi thêm món.
                if (order.CouponId.HasValue && orderDto.CouponId.HasValue)
                {
                    throw new ArgumentException("Bàn này đã áp mã giảm giá rồi.");
                }

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
                    Description = $"Khách đã thêm món vào đơn {order.OrderCode} tại bàn {orderDto.TableId}."
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
                    Note = "Khởi tạo đơn hàng mới"
                });

                await _context.ActivityLogs.AddAsync(new ActivityLog
                {
                    Action = "create_order",
                    Description = $"Tạo đơn hàng mới {order.OrderCode} tại bàn {orderDto.TableId}."
                });


            }

            // 2. Process items + inventory
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
                        throw new ArgumentException($"Sản phẩm '{product.Name}' chỉ còn {product.Inventory.Value} phần.");

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

                    foreach (var comboItem in combo.ComboItems)
                    {
                        var product = comboItem.Product;
                        int requiredQty = (comboItem.Quantity ?? 1) * itemDto.Quantity;
                        
                        if (product.Inventory.HasValue && product.Inventory.Value < requiredQty)
                        {
                            throw new ArgumentException($"Sản phẩm '{product.Name}' trong combo '{combo.Name}' không đủ số lượng. Cần: {requiredQty}, Còn: {product.Inventory.Value}.");
                        }
                    }

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
            }

            // Nếu tạo mới coupon cho đơn này, gắn coupon trước rồi tính lại tài chính từ item thực tế.
            var isApplyingNewCoupon = orderDto.CouponId.HasValue && !order.CouponId.HasValue;
            if (isApplyingNewCoupon)
            {
                order.CouponId = orderDto.CouponId;
                order.Coupon = await _context.Coupons.FindAsync(orderDto.CouponId!.Value);
            }

            await RecalculateOrderFinancialsAsync(order);

            // Chỉ tăng UsedCount khi coupon thực sự còn hợp lệ và được giữ lại sau khi recalc.
            if (isApplyingNewCoupon && order.CouponId.HasValue)
            {
                var coupon = order.Coupon ?? await _context.Coupons.FindAsync(order.CouponId.Value);
                if (coupon != null) coupon.UsedCount++;
            }

            // 3. Update table status
            table.Status = TableStatus.Taken;

            var staffNotif = new Notification
            {
                Message = $"🔔 Bàn {orderDto.TableId} vừa đặt món mới! Đơn: {order.OrderCode}",
                Type = "new_order",
                TargetRole = "staff",
                CreatedAt = DateTime.Now
            };
            var kitchenNotif = new Notification
            {
                Message = $"🔔 Bàn {orderDto.TableId} vừa đặt món mới! Đơn: {order.OrderCode}",
                Type = "new_order",
                TargetRole = "kitchen",
                CreatedAt = DateTime.Now
            };
            _context.Notifications.AddRange(staffNotif, kitchenNotif);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // === SignalR Broadcast ===
            var orderPayload = new { orderId = order.Id, orderCode = order.OrderCode, tableId = orderDto.TableId };
            await _hubContext.Clients.Group("kitchen").SendAsync("NewOrderReceived", orderPayload);
            await _hubContext.Clients.Group("staff").SendAsync("NewOrderReceived", orderPayload);

            await _hubContext.Clients.Group("staff").SendAsync("NewNotification", new
            {
                id = staffNotif.Id,
                message = staffNotif.Message,
                type = staffNotif.Type,
                title = $"Đơn mới từ Bàn {orderDto.TableId}"
            });
            await _hubContext.Clients.Group("kitchen").SendAsync("NewNotification", new
            {
                id = kitchenNotif.Id,
                message = kitchenNotif.Message,
                type = kitchenNotif.Type,
                title = $"Đơn mới từ Bàn {orderDto.TableId}"
            });

            return order;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
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
                .Include(o => o.Coupon)
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
                CouponId = order.CouponId,
                CouponCode = order.Coupon?.Code,
                DiscountAmount = order.DiscountAmount,
                Items = order.OrderItems.Select(oi => new OrderItemDetailDto
                {
                    Id = oi.Id,
                    Name = oi.Product?.Name ?? oi.Combo?.Name ?? "Không xác định",
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
            var availableTables = await _context.OrderTables
                .CountAsync(t => t.Status == TableStatus.Available);
            var cleaningTables = await _context.OrderTables
                .CountAsync(t => t.Status == TableStatus.Cleaning);

            var today = DateTime.Today;
            var todayOrders = await _context.Orders
                .Where(o => o.CreatedAt >= today)
                .ToListAsync();

            var hourlyTraffic = todayOrders
                .Where(o => o.CreatedAt.HasValue)
                .GroupBy(o => o.CreatedAt.Value.Hour)
                .Select(g => new { hour = g.Key, count = g.Count() })
                .ToList();

            return new { totalOrders, totalRevenue, activeTables, availableTables, cleaningTables, hourlyTraffic = hourlyTraffic };
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
                Description = $"Đã hủy đơn {order.OrderCode}. Lý do: {reason ?? "Không có"}."
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelOrderItemAsync(int orderItemId, string? reason)
        {
            var item = await _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Order)
                    .ThenInclude(o => o.Coupon)
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

            // Recalculate order total + coupon discount after item changes
            if (item.Order != null)
            {
                await RecalculateOrderFinancialsAsync(item.Order);
            }

            await _context.ActivityLogs.AddAsync(new ActivityLog
            {
                Action = "cancel_item",
                Description = $"Đã hủy món '{item.Product?.Name ?? "Món không xác định"}' (mã món: {orderItemId}) trong đơn {(item.Order?.OrderCode ?? "không xác định")}. Lý do: {reason ?? "Không có"}."
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
                Description = $"Đơn {order.OrderCode} đã chuyển từ bàn {(oldTable != null ? oldTable.TableNumber : "không xác định")} sang bàn {newTable.TableNumber}."
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MergeOrderAsync(int targetOrderId, int sourceOrderId)
        {
            if (targetOrderId == sourceOrderId) return false;

            var targetOrder = await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.Coupon)
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.Id == targetOrderId);

            var sourceOrder = await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.Coupon)
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.Id == sourceOrderId);

            if (targetOrder == null || sourceOrder == null) return false;

            string[] activeStatuses = { OrderStatus.Pending, OrderStatus.Processing, OrderStatus.Ready, OrderStatus.Served };
            if (!activeStatuses.Contains(targetOrder.Status?.ToLower()) || !activeStatuses.Contains(sourceOrder.Status?.ToLower()))
                return false;

            // Chuyển toàn bộ Item từ Source sang Target
            foreach (var item in sourceOrder.OrderItems.ToList())
            {
                item.OrderId = targetOrderId;
                targetOrder.OrderItems.Add(item);
            }

            // Rule merge coupon:
            // - Cả 2 đơn đều có coupon => bỏ coupon (đơn gộp không áp coupon)
            // - Chỉ 1 đơn có coupon => giữ coupon đó, nhưng phải recalc theo tổng bill mới + min order
            var targetHasCoupon = targetOrder.CouponId.HasValue;
            var sourceHasCoupon = sourceOrder.CouponId.HasValue;

            if (targetHasCoupon && sourceHasCoupon)
            {
                await ReleaseCouponUsageAsync(targetOrder.CouponId);
                await ReleaseCouponUsageAsync(sourceOrder.CouponId);
                targetOrder.CouponId = null;
                targetOrder.Coupon = null;
                targetOrder.DiscountAmount = null;
            }
            else if (!targetHasCoupon && sourceHasCoupon)
            {
                targetOrder.CouponId = sourceOrder.CouponId;
                targetOrder.Coupon = sourceOrder.Coupon;
            }

            // Tính lại tổng tiền/giảm giá dựa trên item thực tế sau khi gộp
            await RecalculateOrderFinancialsAsync(targetOrder);

            // Xử lý Source Order
            string oldStatus = sourceOrder.Status!;
            sourceOrder.Status = OrderStatus.Cancelled;
            sourceOrder.TotalAmount = 0;
            sourceOrder.DiscountAmount = null;
            sourceOrder.CouponId = null;
            sourceOrder.UpdatedAt = DateTime.Now;

            await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
            {
                Order = sourceOrder,
                OldStatus = oldStatus,
                NewStatus = OrderStatus.Cancelled,
                Note = $"Đã gộp vào đơn {targetOrder.OrderCode}"
            });

            // Ghi log vào Target
            await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
            {
                Order = targetOrder,
                NewStatus = targetOrder.Status,
                Note = $"Đã gộp từ đơn {sourceOrder.OrderCode}"
            });

            // Xử lý Table của Source
            if (sourceOrder.Table != null)
            {
                bool hasOtherActiveOrders = await _context.Orders.AnyAsync(o =>
                    o.TableId == sourceOrder.Table.Id && o.Id != sourceOrder.Id &&
                    activeStatuses.Contains(o.Status!.ToLower()));
                
                if (!hasOtherActiveOrders)
                {
                    sourceOrder.Table.Status = TableStatus.Available;
                }
            }

            await _context.ActivityLogs.AddAsync(new ActivityLog
            {
                Action = "merge_order",
                Description = $"Đã gộp đơn {sourceOrder.OrderCode} vào đơn {targetOrder.OrderCode}."
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Order>> GetOrdersAsync(int limit = 10)
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .OrderByDescending(o => o.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<object> GetTopProductsReportAsync(DateTime? start, DateTime? end)
        {
            var query = _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .Where(oi => oi.Order != null && oi.Order.Status.ToLower() == OrderStatus.Paid && oi.ProductId != null);

            if (start.HasValue) query = query.Where(oi => oi.Order!.CreatedAt >= start.Value.Date);
            if (end.HasValue) query = query.Where(oi => oi.Order!.CreatedAt <= end.Value.Date.AddDays(1).AddTicks(-1));

            return await query
                .GroupBy(oi => oi.Product!.Name)
                .Select(g => new {
                    Name = g.Key,
                    Quantity = g.Sum(oi => oi.Quantity ?? 1),
                    Revenue = g.Sum(oi => (oi.Quantity ?? 1) * oi.UnitPrice)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(10)
                .ToListAsync();
        }

        public async Task<object> GetCategorySalesReportAsync(DateTime? start, DateTime? end)
        {
            var query = _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product).ThenInclude(p => p.Category)
                .Where(oi => oi.Order != null && oi.Order.Status.ToLower() == OrderStatus.Paid && oi.Product != null && oi.Product.Category != null);

            if (start.HasValue) query = query.Where(oi => oi.Order!.CreatedAt >= start.Value.Date);
            if (end.HasValue) query = query.Where(oi => oi.Order!.CreatedAt <= end.Value.Date.AddDays(1).AddTicks(-1));

            return await query
                .GroupBy(oi => oi.Product!.Category!.Name)
                .Select(g => new {
                    Category = g.Key,
                    Revenue = g.Sum(oi => (oi.Quantity ?? 1) * oi.UnitPrice)
                })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync();
        }
    }
}
