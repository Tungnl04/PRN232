using FoodQR.API.DTOs;
using FoodQR.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodQR.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly FoodStoreDbContext _context;

        public OrdersController(FoodStoreDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult<Order>> CreateOrder(CreateOrderDto orderDto)
        {
            // 1. Check for active order on this table
            var activeOrder = await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.TableId == orderDto.TableId && 
                           !new[] { "paid", "cancelled", "rejected" }.Contains(o.Status))
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            Order order;
            if (activeOrder != null)
            {
                order = activeOrder;
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
            }

            decimal additionalAmount = 0;
            foreach (var itemDto in orderDto.Items)
            {
                decimal unitPrice = 0;
                if (itemDto.ProductId.HasValue)
                {
                    var product = await _context.Products.FindAsync(itemDto.ProductId.Value);
                    if (product == null || product.IsAvailable == false) continue; // Skip out of stock
                    unitPrice = product.Price;
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

        [HttpGet("table/{tableId}/active")]
        public async Task<ActionResult<Order>> GetActiveOrderByTable(int tableId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.TableId == tableId && o.Status != "completed" && o.Status != "cancelled")
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (order == null) return NotFound();
            return order;
        }
    }
}
