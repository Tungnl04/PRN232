using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoodQR.API.Core.Entities;

public class Coupon
{
    public int Id { get; set; }

    [Required]
    public string Code { get; set; } = null!;

    [Required]
    public string DiscountType { get; set; } = "Percent"; // "Percent" or "Fixed"

    [Range(0, 50000000)]
    public decimal DiscountValue { get; set; }

    public decimal MinOrderAmount { get; set; } = 0;

    public int? MaxUsage { get; set; }

    public int UsedCount { get; set; } = 0;

    public DateTime? ExpiryDate { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
