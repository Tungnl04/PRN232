using FoodQR.API.Core.Entities;
using FoodQR.API.Core.Enums;
using FoodQR.API.Core.Interfaces;
using FoodQR.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodQR.API.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly FoodStoreDbContext _context;
        private readonly IConfiguration _configuration;

        public PaymentService(FoodStoreDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<(bool Success, string? Error, object? Result)> ProcessPaymentAsync(int orderId, string method, bool simulateSuccess = true)
        {
            var order = await _context.Orders
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return (false, "Không tìm thấy đơn hàng.", null);

            // Validate order status
            var allowedStatuses = new[] { OrderStatus.Ready, OrderStatus.Served };
            if (!allowedStatuses.Contains(order.Status?.ToLower()))
                return (false, $"Chỉ có thể thanh toán đơn ở trạng thái sẵn sàng hoặc đã phục vụ. Trạng thái hiện tại: {order.Status}", null);

            if (order.PaymentStatus == PaymentStatus.Success)
                return (false, "Đơn hàng này đã được thanh toán rồi.", null);

            if (order.PaymentStatus == PaymentStatus.Expired)
                return (false, "Phiên thanh toán đã hết hạn. Vui lòng tạo phiên mới.", null);

            order.PaymentMethod = method;
            string oldStatus = order.Status ?? OrderStatus.Pending;

            if (simulateSuccess)
            {
                order.PaymentStatus = PaymentStatus.Success;
                order.Status = OrderStatus.Paid;
                order.UpdatedAt = DateTime.Now;

                if (order.Table != null)
                    order.Table.Status = TableStatus.Cleaning;

                await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
                {
                    Order = order, OldStatus = oldStatus, NewStatus = OrderStatus.Paid,
                    Note = $"Thanh toán thành công qua phương thức {method}"
                });

                await _context.ActivityLogs.AddAsync(new ActivityLog
                {
                    Action = "payment_success",
                    Description = $"Đơn {order.OrderCode} đã thanh toán thành công qua phương thức {method}."
                });

                await _context.Notifications.AddAsync(new Notification
                {
                    Message = $"Đơn {order.OrderCode} đã thanh toán thành công!",
                    Type = "payment_success",
                    TargetRole = AppRoles.Admin,
                    CreatedAt = DateTime.Now
                });
            }
            else
            {
                order.PaymentStatus = PaymentStatus.Failed;
                await _context.ActivityLogs.AddAsync(new ActivityLog
                {
                    Action = "payment_failed",
                    Description = $"Thanh toán đơn {order.OrderCode} qua phương thức {method} thất bại."
                });
            }

            await _context.SaveChangesAsync();
            return (true, null, new { order.Status, order.PaymentStatus });
        }

        public async Task<(bool Success, string? Error)> ExpirePaymentAsync(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return (false, "Không tìm thấy đơn hàng.");

            if (order.PaymentStatus != PaymentStatus.Pending && order.PaymentStatus != PaymentStatus.Failed)
                return (false, $"Chỉ có thể hết hạn phiên thanh toán đang chờ hoặc thất bại. Hiện tại: {order.PaymentStatus}");

            order.PaymentStatus = PaymentStatus.Expired;
            await _context.ActivityLogs.AddAsync(new ActivityLog
            {
                Action = "payment_expired",
                Description = $"Phiên thanh toán của đơn {order.OrderCode} đã hết hạn."
            });

            await _context.SaveChangesAsync();
            return (true, null);
        }

        /// <summary>
        /// Tạo VNPay payment URL
        /// </summary>
        public async Task<(bool Success, string? Error, string? PaymentUrl)> CreateVnPayUrlAsync(int orderId, HttpContext httpContext)
        {
            var order = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return (false, "Không tìm thấy đơn hàng.", null);

            // Cho phép tạo URL khi đơn ở trạng thái ready hoặc served
            var allowedStatuses = new[] { OrderStatus.Pending, OrderStatus.Processing, OrderStatus.Preparing, OrderStatus.Ready, OrderStatus.Served };
            if (!allowedStatuses.Contains(order.Status?.ToLower()))
                return (false, $"Không thể tạo thanh toán cho đơn có trạng thái: {order.Status}", null);

            if (order.PaymentStatus == PaymentStatus.Success)
                return (false, "Đơn hàng này đã được thanh toán rồi.", null);

            // Tính tổng tiền bao gồm thuế
            var storeConfig = await _context.StoreConfigurations.FirstOrDefaultAsync();
            decimal subtotal = order.TotalAmount ?? 0;
            decimal taxRate = storeConfig?.TaxRate ?? 0.08m;
            decimal taxAmount = 0;
            decimal totalWithTax;

            if (storeConfig?.IsTaxIncludedInPrice == true)
            {
                // Giá đã bao gồm thuế, tách thuế ra hiển thị nhưng tổng vẫn giữ nguyên
                totalWithTax = subtotal;
            }
            else
            {
                // Thuế cộng thêm vào
                taxAmount = Math.Round(subtotal * taxRate, 0);
                totalWithTax = subtotal + taxAmount;
            }

            if (totalWithTax <= 0)
                return (false, "Tổng thanh toán phải lớn hơn 0.", null);

            // VNPay config
            var vnpBaseUrl = _configuration["VnPay:BaseUrl"]!;
            var tmnCode = _configuration["VnPay:TmnCode"]!;
            var hashSecret = _configuration["VnPay:HashSecret"]!;
            var returnUrl = _configuration["VnPay:ReturnUrl"]!;

            // IP address
            var ipAddress = httpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1";

            var paymentUrl = VnPayHelper.CreatePaymentUrl(
                baseUrl: vnpBaseUrl,
                tmnCode: tmnCode,
                hashSecret: hashSecret,
                returnUrl: returnUrl,
                txnRef: order.OrderCode,
                amount: totalWithTax,
                orderInfo: $"Thanh toan don hang {order.OrderCode}",
                ipAddress: ipAddress
            );

            // Log activity
            await _context.ActivityLogs.AddAsync(new ActivityLog
            {
                Action = "vnpay_url_created",
                Description = $"Đã tạo liên kết thanh toán VNPay cho đơn {order.OrderCode} - Số tiền: {totalWithTax:N0} VNĐ."
            });
            await _context.SaveChangesAsync();

            return (true, null, paymentUrl);
        }

        /// <summary>
        /// Xử lý VNPay return callback
        /// </summary>
        public async Task<(bool Success, string? Error, string? OrderCode)> VnPayReturnAsync(IQueryCollection query)
        {
            var hashSecret = _configuration["VnPay:HashSecret"]!;

            // Verify signature
            bool isValidSignature = VnPayHelper.ValidateSignature(query, hashSecret);
            if (!isValidSignature)
                return (false, "Chữ ký không hợp lệ.", null);

            var responseCode = query["vnp_ResponseCode"].ToString();
            var txnRef = query["vnp_TxnRef"].ToString();
            var vnpTransactionNo = query["vnp_TransactionNo"].ToString();

            // Tìm order
            var order = await _context.Orders
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.OrderCode == txnRef);

            if (order == null)
                return (false, "Không tìm thấy đơn hàng.", txnRef);

            // Nếu đã paid rồi thì skip (tránh xử lý duplicate)
            if (order.PaymentStatus == PaymentStatus.Success)
                return (true, null, order.OrderCode);

            string oldStatus = order.Status ?? OrderStatus.Pending;

            if (responseCode == "00")
            {
                // Thanh toán thành công
                order.PaymentStatus = PaymentStatus.Success;
                order.PaymentMethod = "vnpay";
                order.Status = OrderStatus.Paid;
                order.UpdatedAt = DateTime.Now;

                if (order.Table != null)
                    order.Table.Status = TableStatus.Cleaning;

                await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
                {
                    Order = order,
                    OldStatus = oldStatus,
                    NewStatus = OrderStatus.Paid,
                    Note = $"Thanh toán VNPay thành công - Mã giao dịch: {vnpTransactionNo}"
                });

                await _context.ActivityLogs.AddAsync(new ActivityLog
                {
                    Action = "payment_success",
                    Description = $"Đơn {order.OrderCode} đã thanh toán VNPay thành công - Mã giao dịch: {vnpTransactionNo}."
                });

                await _context.Notifications.AddAsync(new Notification
                {
                    Message = $"✅ Đơn {order.OrderCode} đã thanh toán VNPay thành công!",
                    Type = "payment_success",
                    TargetRole = AppRoles.Staff,
                    CreatedAt = DateTime.Now
                });

                await _context.Notifications.AddAsync(new Notification
                {
                    Message = $"💳 Đơn {order.OrderCode} thanh toán VNPay - {vnpTransactionNo}",
                    Type = "payment_success",
                    TargetRole = AppRoles.Admin,
                    CreatedAt = DateTime.Now
                });
            }
            else
            {
                // Thanh toán thất bại
                order.PaymentStatus = PaymentStatus.Failed;

                await _context.ActivityLogs.AddAsync(new ActivityLog
                {
                    Action = "payment_failed",
                    Description = $"Thanh toán VNPay của đơn {order.OrderCode} thất bại - Mã lỗi: {responseCode}."
                });
            }

            await _context.SaveChangesAsync();
            return (responseCode == "00", responseCode != "00" ? VnPayHelper.GetResponseMessage(responseCode) : null, order.OrderCode);
        }
    }
}
