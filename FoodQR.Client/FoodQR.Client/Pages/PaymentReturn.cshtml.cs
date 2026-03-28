using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodQR.Client.Pages
{
    public class PaymentReturnModel : PageModel
    {
        public bool IsSuccess { get; set; }
        public string OrderCode { get; set; } = "";
        public string FormattedAmount { get; set; } = "0 ₫";
        public string TransactionNo { get; set; } = "";
        public string ErrorMessage { get; set; } = "";

        public void OnGet(
            bool success = false,
            string? orderCode = null,
            string? responseCode = null,
            string? amount = null,
            string? transactionNo = null,
            string? error = null)
        {
            IsSuccess = success;
            OrderCode = orderCode ?? "";
            TransactionNo = transactionNo ?? "";
            ErrorMessage = error ?? "Giao dịch không thành công. Vui lòng thử lại.";

            // VNPay amount is multiplied by 100
            if (long.TryParse(amount, out var amountValue))
            {
                FormattedAmount = (amountValue / 100m).ToString("N0") + " ₫";
            }
        }
    }
}
