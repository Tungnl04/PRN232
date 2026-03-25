using FoodQR.API.Application.DTOs;
using FoodQR.API.Core.Entities;
using FoodQR.API.Core.Interfaces;
using FoodQR.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodQR.API.Core.Enums;

namespace FoodQR.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly FoodStoreDbContext _context;
        private readonly IOrderService _orderService;

        public OrdersController(FoodStoreDbContext context, IOrderService orderService)
        {
            _context = context;
            _orderService = orderService;
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult<Order>> CreateOrder(OrderCreateDto orderDto)
        {
            if (orderDto.TableId <= 0) return BadRequest("Table ID is required.");
            if (orderDto.Items == null || !orderDto.Items.Any()) return BadRequest("Order must have at least one item.");

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

                // Business Rule: Nếu đơn đang Ready/Served mà đặt gọi thêm, phải đưa về Pending
                if (string.Equals(order.Status, "ready", StringComparison.OrdinalIgnoreCase) || 
                    string.Equals(order.Status, "served", StringComparison.OrdinalIgnoreCase))
                {
                    string oldStatus = order.Status;
                    order.Status = "pending";
                    order.UpdatedAt = DateTime.Now;

                    await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
                    {
                        Order = order,
                        OldStatus = oldStatus,
                        NewStatus = "pending",
                        Note = "Khách đặt thêm món mới"
                    });
                }

                await _context.ActivityLogs.AddAsync(new ActivityLog { 
                    Action = "append_items", 
                    Description = $"Guest appended items to Order {order.OrderCode} at Table {orderDto.TableId}" 
                });
            }
            else
            {
                // Create new customer
                var customer = new Customer { Name = orderDto.CustomerName, Email = orderDto.CustomerEmail };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                order = new Order
                {
                    OrderCode = "ORD-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                    CustomerId = customer.Id,
                    TableId = orderDto.TableId,
                    Status = "pending",
                    PaymentStatus = "pending",
                    CreatedAt = DateTime.Now
                };
                _context.Orders.Add(order);

                await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory {
                    Order = order,
                    NewStatus = "pending",
                    Note = "Order initiated"
                });

                await _context.ActivityLogs.AddAsync(new ActivityLog { 
                    Action = "create_order", 
                    Description = $"New order {order.OrderCode} created at Table {orderDto.TableId}" 
                });

                await _context.Notifications.AddAsync(new Notification {
                    Message = $"🔔 Bàn {orderDto.TableId} vừa đặt món mới! Đơn: {order.OrderCode}",
                    Type = "new_order",
                    TargetRole = "staff",
                    CreatedAt = DateTime.Now
                });
            }

            decimal additionalAmount = 0;
            foreach (var itemDto in orderDto.Items)
            {
                // Normalize: 0 → null (tránh FK constraint error)
                if (itemDto.ProductId == 0) itemDto.ProductId = null;
                if (itemDto.ComboId == 0) itemDto.ComboId = null;

                // Validate: phải có ít nhất 1 trong 2
                if (!itemDto.ProductId.HasValue && !itemDto.ComboId.HasValue) continue;

                decimal unitPrice = 0;
                if (itemDto.ProductId.HasValue)
                {
                    var product = await _context.Products.FindAsync(itemDto.ProductId.Value);
                    if (product == null || product.IsAvailable == false) continue; // Skip out of stock

                    // Kiểm tra tồn kho
                    if (product.Inventory.HasValue && product.Inventory.Value < itemDto.Quantity)
                    {
                        continue; // Không đủ hàng → skip
                    }

                    unitPrice = product.Price;

                    // Trừ kho
                    if (product.Inventory.HasValue)
                    {
                        product.Inventory -= itemDto.Quantity;
                        if (product.Inventory <= 0)
                        {
                            product.Inventory = 0;
                            product.IsAvailable = false;
                            await _context.Notifications.AddAsync(new Notification
                            {
                                Message = $"⚠️ Sản phẩm '{product.Name}' đã hết hàng! (inventory = 0)",
                                Type = "inventory_alert",
                                TargetRole = "admin",
                                CreatedAt = DateTime.Now
                            });
                        }
                    }
                }
                else if (itemDto.ComboId.HasValue)
                {
                    var combo = await _context.Combos.FindAsync(itemDto.ComboId.Value);
                    if (combo == null || combo.Available == false) continue;
                    unitPrice = combo.Price;
                }

                var orderItem = new OrderItem
                {
                    ProductId = itemDto.ProductId,
                    ComboId = itemDto.ComboId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = unitPrice,
                    Status = "pending",
                    Note = itemDto.Note
                };
                order.OrderItems.Add(orderItem);
                additionalAmount += unitPrice * itemDto.Quantity;
            }

            order.TotalAmount = (order.TotalAmount ?? 0) + additionalAmount;

            // 3. Update table status
            var table = await _context.OrderTables.FindAsync(orderDto.TableId);
            if (table != null) table.Status = "taken";

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }

        [Authorize(Roles = "staff,admin")]
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return order;
        }

        [AllowAnonymous]
        [HttpGet("active/{tableId}")]
        public async Task<ActionResult<OrderDetailDto>> GetActiveOrderByTable(int tableId)
        {
            var order = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Combo)
                .Where(o => o.TableId == tableId && !new[] { OrderStatus.Paid, OrderStatus.Cancelled, OrderStatus.Rejected }.Contains(o.Status))
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (order == null) return NotFound();

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
                    Status = oi.Status ?? "pending",
                    Note = oi.Note
                }).ToList()
            };
        }

        [Authorize(Roles = "staff,admin")]
        [HttpGet("stats/overview")]
        public async Task<ActionResult<object>> GetDashboardStats()
        {
            var totalOrders = await _context.Orders.CountAsync();
            var totalRevenue = await _context.Orders
                .Where(o => o.Status.ToLower() == OrderStatus.Paid)
                .SumAsync(o => o.TotalAmount ?? 0);
            var activeTables = await _context.OrderTables
                .CountAsync(t => t.Status.ToLower() == "taken");

            return new { 
                totalOrders, 
                totalRevenue, 
                activeTables 
            };
        }

        [Authorize(Roles = "staff,admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelOrder(int id, [FromQuery] string? reason)
        {
            var result = await _orderService.CancelOrderAsync(id, reason);
            if (!result) return BadRequest(new { Error = "Không thể hủy đơn. Chỉ hủy được đơn pending/processing." });
            return Ok(new { Message = "Đơn hàng đã được hủy." });
        }

        [AllowAnonymous]
        [HttpDelete("items/{itemId}")]
        public async Task<IActionResult> CancelOrderItem(int itemId, [FromQuery] string? reason)
        {
            var result = await _orderService.CancelOrderItemAsync(itemId, reason);
            if (!result) return BadRequest(new { Error = "Không thể hủy món. Chỉ hủy được món đang pending." });
            return Ok(new { Message = "Món đã được hủy." });
        }
    }
}
