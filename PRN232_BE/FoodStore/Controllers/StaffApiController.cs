using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FoodStoreAPI.Interfaces;

namespace FoodStoreAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    //[Authorize(Roles = "staff,admin")]
    public class StaffApiController : ControllerBase
    {
        private readonly IStaffService _staffService;
        private readonly ILogger<StaffApiController> _logger;

        public StaffApiController(IStaffService staffService, ILogger<StaffApiController> logger)
        {
            _staffService = staffService;
            _logger = logger;
        }

        [HttpGet("orders")]
        public async Task<ActionResult> GetAllOrders()
        {
            var result = await _staffService.GetAllOrdersAsync();

            if (!result.Success)
            {
                _logger.LogError("Lỗi khi lấy danh sách orders: {Message}", result.Message);
                return StatusCode(500, new
                {
                    success = false,
                    message = result.Message,
                    error = result.Error
                });
            }

            return Ok(new
            {
                success = result.Success,
                data = result.Data,
                count = result.Count,
                message = result.Message
            });
        }

        [HttpGet("orders/{status}")]
        public async Task<ActionResult> GetOrdersByStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Status không được để trống"
                });
            }

            var result = await _staffService.GetOrdersByStatusAsync(status);

            if (!result.Success)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = result.Message,
                    error = result.Error
                });
            }

            return Ok(new
            {
                success = result.Success,
                data = result.Data,
                count = result.Count,
                message = result.Message
            });
        }

        [HttpGet("order/{id}")]
        public async Task<ActionResult> GetOrderById(decimal id)
        {
            var result = await _staffService.GetOrderByIdAsync(id);

            if (!result.Success)
            {
                if (result.Message.Contains("không hợp lệ"))
                    return BadRequest(new { success = false, message = result.Message });

                if (result.Message.Contains("Không tìm thấy"))
                    return NotFound(new { success = false, message = result.Message });

                return StatusCode(500, new
                {
                    success = false,
                    message = result.Message,
                    error = result.Error
                });
            }

            return Ok(new
            {
                success = result.Success,
                data = result.Data,
                message = result.Message
            });
        }

        [HttpPut("order/{id}/status")]
        public async Task<ActionResult> UpdateOrderStatus(decimal id, [FromBody] UpdateOrderStatusRequest request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Request body không được để trống" });

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new
                {
                    success = false,
                    message = "Dữ liệu không hợp lệ",
                    errors = errors
                });
            }

            var result = await _staffService.UpdateOrderStatusAsync(id, request.Status);

            if (!result.Success)
            {
                if (result.Message.Contains("không hợp lệ") || result.Message.Contains("không được để trống"))
                    return BadRequest(new { success = false, message = result.Message });

                if (result.Message.Contains("Không tìm thấy"))
                    return NotFound(new { success = false, message = result.Message });

                return StatusCode(500, new
                {
                    success = false,
                    message = result.Message,
                    error = result.Error
                });
            }

            return Ok(new
            {
                success = result.Success,
                data = result.Data,
                message = result.Message
            });
        }
    }

    public class UpdateOrderStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}