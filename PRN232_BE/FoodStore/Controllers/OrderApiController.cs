using Microsoft.AspNetCore.Mvc;
using FoodStoreAPI.DTOs;
using FoodStoreAPI.Interfaces;

namespace FoodStoreAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrderApiController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrderApiController> _logger;

        public OrderApiController(IOrderService orderService, ILogger<OrderApiController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        [HttpPost("orders")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDTO request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Request body không được để trống" });

            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });

            try
            {
                var result = await _orderService.CreateOrderAsync(request);

                if (!result.Success)
                    return BadRequest(new { success = false, message = result.Message });

                return Ok(new
                {
                    success = true,
                    data = new { orderCode = result.OrderCode },
                    message = "Đặt món thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo đơn hàng");
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra", error = ex.Message });
            }
        }

        [HttpGet("orders/{orderCode}")]
        public async Task<IActionResult> GetOrdersByOrderCode(string orderCode)
        {
            if (string.IsNullOrWhiteSpace(orderCode))
                return BadRequest(new { success = false, message = "Order Code không được để trống" });

            try
            {
                var result = await _orderService.GetOrdersByOrderCodeAsync(orderCode);

                if (!result.Success)
                    return NotFound(new { success = false, message = result.Message });

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        customerName = result.CustomerName,
                        orders = result.Orders
                    },
                    count = result.Orders?.Count ?? 0,
                    message = $"Tìm thấy {result.Orders?.Count ?? 0} đơn hàng"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tìm đơn hàng theo mã");
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi tìm kiếm đơn hàng", error = ex.Message });
            }
        }
    }
}