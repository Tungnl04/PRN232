using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace FoodQR.API.Application.Services
{
    /// <summary>
    /// Helper tự viết cho VNPay: tạo payment URL và verify HMAC-SHA512 signature.
    /// Không dùng thư viện ngoài.
    /// </summary>
    public static class VnPayHelper
    {
        /// <summary>
        /// Tạo payment URL để redirect khách sang VNPay
        /// </summary>
        public static string CreatePaymentUrl(
            string baseUrl,
            string tmnCode,
            string hashSecret,
            string returnUrl,
            string txnRef,
            decimal amount,
            string orderInfo,
            string ipAddress,
            string locale = "vn")
        {
            var vnpParams = new SortedDictionary<string, string>
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", tmnCode },
                { "vnp_Amount", ((long)(amount * 100)).ToString() }, // VNPay yêu cầu nhân 100
                { "vnp_CurrCode", "VND" },
                { "vnp_TxnRef", txnRef },
                { "vnp_OrderInfo", orderInfo },
                { "vnp_OrderType", "food_order" },
                { "vnp_Locale", locale },
                { "vnp_ReturnUrl", returnUrl },
                { "vnp_IpAddr", ipAddress },
                { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") }
            };

            // Build query string (sorted)
            var queryBuilder = new StringBuilder();
            foreach (var kv in vnpParams)
            {
                if (queryBuilder.Length > 0) queryBuilder.Append('&');
                queryBuilder.Append(WebUtility.UrlEncode(kv.Key));
                queryBuilder.Append('=');
                queryBuilder.Append(WebUtility.UrlEncode(kv.Value));
            }

            // HMAC-SHA512
            string signData = queryBuilder.ToString();
            string secureHash = HmacSHA512(hashSecret, signData);

            return $"{baseUrl}?{signData}&vnp_SecureHash={secureHash}";
        }

        /// <summary>
        /// Verify HMAC-SHA512 signature từ VNPay callback/IPN
        /// </summary>
        public static bool ValidateSignature(IQueryCollection query, string hashSecret)
        {
            var vnpSecureHash = query["vnp_SecureHash"].ToString();
            if (string.IsNullOrEmpty(vnpSecureHash)) return false;

            // Lấy tất cả params trừ vnp_SecureHash và vnp_SecureHashType, sort theo key
            var sortedParams = new SortedDictionary<string, string>();
            foreach (var key in query.Keys)
            {
                if (key == "vnp_SecureHash" || key == "vnp_SecureHashType") continue;
                var value = query[key].ToString();
                if (!string.IsNullOrEmpty(value))
                {
                    sortedParams.Add(key, value);
                }
            }

            var signDataBuilder = new StringBuilder();
            foreach (var kv in sortedParams)
            {
                if (signDataBuilder.Length > 0) signDataBuilder.Append('&');
                signDataBuilder.Append(WebUtility.UrlEncode(kv.Key));
                signDataBuilder.Append('=');
                signDataBuilder.Append(WebUtility.UrlEncode(kv.Value));
            }

            string computedHash = HmacSHA512(hashSecret, signDataBuilder.ToString());
            return string.Equals(computedHash, vnpSecureHash, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Lấy response code message
        /// </summary>
        public static string GetResponseMessage(string responseCode)
        {
            return responseCode switch
            {
                "00" => "Giao dịch thành công",
                "07" => "Trừ tiền thành công. Giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường)",
                "09" => "Thẻ/Tài khoản chưa đăng ký dịch vụ InternetBanking",
                "10" => "Xác thực thông tin thẻ/tài khoản không đúng quá 3 lần",
                "11" => "Đã hết hạn chờ thanh toán",
                "12" => "Thẻ/Tài khoản bị khóa",
                "13" => "Nhập sai mật khẩu xác thực giao dịch (OTP)",
                "24" => "Khách hàng hủy giao dịch",
                "51" => "Tài khoản không đủ số dư",
                "65" => "Tài khoản đã vượt quá hạn mức giao dịch trong ngày",
                "75" => "Ngân hàng thanh toán đang bảo trì",
                "79" => "Nhập sai mật khẩu thanh toán quá số lần quy định",
                "99" => "Lỗi không xác định",
                _ => $"Lỗi không xác định (Mã: {responseCode})"
            };
        }

        private static string HmacSHA512(string key, string inputData)
        {
            var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(inputData));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}
