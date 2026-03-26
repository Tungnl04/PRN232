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

            var order = await _orderService.CreateOrAppendOrderAsync(orderDto);
            if (order == null) return BadRequest("Could not create order.");

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
                .Include(o => o.Customer)
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
                CustomerName = order.Customer?.Name,
                CustomerEmail = order.Customer?.Email,
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
            var today = DateTime.Today;
            var totalOrders = await _context.Orders.CountAsync();
            var totalRevenue = await _context.Orders.Where(o => o.Status.ToLower() == OrderStatus.Paid).SumAsync(o => o.TotalAmount ?? 0);
            
            var todayOrders = await _context.Orders.CountAsync(o => o.CreatedAt >= today);
            var todayRevenue = await _context.Orders.Where(o => o.Status.ToLower() == OrderStatus.Paid && o.CreatedAt >= today).SumAsync(o => o.TotalAmount ?? 0);

            var activeTables = await _context.OrderTables.CountAsync(t => t.Status.ToLower() == "taken");
            var cleaningTables = await _context.OrderTables.CountAsync(t => t.Status.ToLower() == "cleaning");
            var availableTables = await _context.OrderTables.CountAsync(t => t.Status.ToLower() == "available");

            // Hourly traffic for today
            var hourlyTraffic = await _context.Orders
                .Where(o => o.CreatedAt >= today)
                .GroupBy(o => o.CreatedAt!.Value.Hour)
                .Select(g => new { Hour = g.Key, Count = g.Count() })
                .OrderBy(x => x.Hour)
                .ToListAsync();

            return new
            {
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                TodayOrders = todayOrders,
                TodayRevenue = todayRevenue,
                ActiveTables = activeTables,
                CleaningTables = cleaningTables,
                AvailableTables = availableTables,
                HourlyTraffic = hourlyTraffic
            };
        }

        [Authorize(Roles = "admin")]
        [HttpGet("reports/revenue")]
        public async Task<ActionResult<object>> GetRevenueReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var query = _context.Orders
                .Where(o => o.Status.ToLower() == OrderStatus.Paid);

            if (startDate.HasValue)
                query = query.Where(o => o.CreatedAt >= startDate.Value.Date);
            
            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(o => o.CreatedAt <= end);
            }

            var totalOrders = await query.CountAsync();
            var totalRevenue = await query.SumAsync(o => o.TotalAmount ?? 0);

            var rawData = await query
                .GroupBy(o => o.CreatedAt!.Value.Date)
                .Select(g => new {
                    Date = g.Key,
                    Revenue = g.Sum(o => o.TotalAmount ?? 0),
                    OrderCount = g.Count()
                })
                .OrderBy(r => r.Date)
                .ToListAsync();

            var revenueByDate = rawData.Select(r => new {
                Date = r.Date.ToString("yyyy-MM-dd"),
                Revenue = r.Revenue,
                OrderCount = r.OrderCount
            }).ToList();

            return new { 
                totalOrders, 
                totalRevenue, 
                revenueByDate 
            };
        }

        [Authorize(Roles = "staff,admin")]
        [HttpPatch("{id}/table/{newTableId}")]
        public async Task<IActionResult> SwitchTable(int id, int newTableId)
        {
            var result = await _orderService.SwitchTableAsync(id, newTableId);
            if (!result) return BadRequest(new { Error = "Không thể chuyển bàn. Bàn mới không trống hoặc đơn hàng không hợp lệ." });
            return Ok(new { Message = "Chuyển bàn thành công." });
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

        [Authorize(Roles = "admin,staff")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders([FromQuery] int limit = 10)
        {
            return await _orderService.GetOrdersAsync(limit);
        }

        [Authorize(Roles = "admin")]
        [HttpGet("reports/top-products")]
        public async Task<ActionResult<object>> GetTopProductsReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            return await _orderService.GetTopProductsReportAsync(startDate, endDate);
        }

        [Authorize(Roles = "admin")]
        [HttpGet("reports/category-sales")]
        public async Task<ActionResult<object>> GetCategorySalesReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            return await _orderService.GetCategorySalesReportAsync(startDate, endDate);
        }
    }
}
