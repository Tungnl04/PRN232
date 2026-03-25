using System;
using System.Collections.Generic;

namespace FoodQR.API.Core.Entities;

public partial class OrderStatusHistory
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public string? OldStatus { get; set; }

    public string? NewStatus { get; set; }

    public int? ChangedBy { get; set; }

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;
}
