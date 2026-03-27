using FoodQR.API.Application.Services;
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
        private readonly IConfiguration _configuration;

        public PaymentsController(IPaymentService paymentService, IConfiguration configuration)
        {
            _paymentService = paymentService;
            _configuration = configuration;
        }

        /// <summary>
        /// Thanh toán tiền mặt (Staff xác nhận)
        /// </summary>
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

        /// <summary>
        /// Đánh dấu thanh toán hết hạn
        /// </summary>
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

        /// <summary>
        /// Tạo VNPay payment URL (Staff/Admin gọi)
        /// </summary>
        [HttpPost("{orderId}/vnpay-url")]
        public async Task<IActionResult> CreateVnPayUrl(int orderId)
        {
            var result = await _paymentService.CreateVnPayUrlAsync(orderId, HttpContext);

            if (!result.Success)
            {
                return BadRequest(new { Error = result.Error });
            }

            return Ok(new { paymentUrl = result.PaymentUrl });
        }

        /// <summary>
        /// VNPay redirect callback — khách được redirect về đây sau khi thanh toán.
        /// API verify signature → cập nhật đơn hàng → redirect về Client PaymentReturn page.
        /// </summary>
        [HttpGet("vnpay-return")]
        [AllowAnonymous]
        public async Task<IActionResult> VnPayReturn()
        {
            var result = await _paymentService.VnPayReturnAsync(Request.Query);

            // Redirect về Client PaymentReturn page với kết quả
            var clientReturnUrl = _configuration["VnPay:ClientReturnUrl"] ?? "https://localhost:7209/PaymentReturn";
            var responseCode = Request.Query["vnp_ResponseCode"].ToString();
            var amount = Request.Query["vnp_Amount"].ToString();
            var transactionNo = Request.Query["vnp_TransactionNo"].ToString();

            var redirectUrl = $"{clientReturnUrl}?success={result.Success}&orderCode={result.OrderCode}&responseCode={responseCode}&amount={amount}&transactionNo={transactionNo}";
            
            if (!string.IsNullOrEmpty(result.Error))
            {
                redirectUrl += $"&error={Uri.EscapeDataString(result.Error)}";
            }

            return Redirect(redirectUrl);
        }
    }
}
