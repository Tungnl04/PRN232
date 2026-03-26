using FoodQR.API.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodQR.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "staff,admin")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("{orderId}/process")]
        public async Task<IActionResult> ProcessPayment(int orderId, [FromQuery] string method, [FromQuery] bool simulateSuccess = true)
        {
            var result = await _paymentService.ProcessPaymentAsync(orderId, method, simulateSuccess);
            
            if (!result.Success)
            {
                return BadRequest(new { Error = result.Error });
            }

            return Ok(result.Result);
        }

        [HttpPatch("{orderId}/expire")]
        public async Task<IActionResult> ExpirePayment(int orderId)
        {
            var result = await _paymentService.ExpirePaymentAsync(orderId);

            if (!result.Success)
            {
                return BadRequest(new { Error = result.Error });
            }

            return NoContent();
        }
    }
}
