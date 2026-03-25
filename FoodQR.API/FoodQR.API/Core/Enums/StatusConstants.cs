namespace FoodQR.API.Core.Enums
{
    /// <summary>
    /// Trạng thái đơn hàng (Order)
    /// </summary>
    public static class OrderStatus
    {
        public const string Pending = "pending";
        public const string Processing = "processing";
        public const string Preparing = "preparing";
        public const string Ready = "ready";
        public const string Served = "served";
        public const string Paid = "paid";
        public const string Rejected = "rejected";
        public const string Cancelled = "cancelled";
    }

    /// <summary>
    /// Trạng thái thanh toán (Payment)
    /// </summary>
    public static class PaymentStatus
    {
        public const string Pending = "pending";
        public const string Success = "success";
        public const string Failed = "failed";
        public const string Expired = "expired";
    }

    /// <summary>
    /// Trạng thái bàn (Table)
    /// </summary>
    public static class TableStatus
    {
        public const string Available = "available";
        public const string Taken = "taken";
        public const string Reserved = "reserved";
        public const string Cleaning = "cleaning";
    }

    /// <summary>
    /// Trạng thái từng món trong đơn (OrderItem)
    /// </summary>
    public static class OrderItemStatus
    {
        public const string Pending = "pending";
        public const string Preparing = "preparing";
        public const string Ready = "ready";
        public const string Served = "served";
        public const string Cancelled = "cancelled";
    }

    /// <summary>
    /// Roles trong hệ thống
    /// </summary>
    public static class AppRoles
    {
        public const string Admin = "admin";
        public const string Staff = "staff";
        public const string Kitchen = "kitchen";
    }
}
