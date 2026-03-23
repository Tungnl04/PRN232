using System;
using System.Collections.Generic;

namespace FoodStoreRepository.Models;

public partial class OrderTable
{
    public int Id { get; set; }

    public string TableNumber { get; set; } = null!;

    public int Capacity { get; set; }

    public string? Status { get; set; }

    public string? Location { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
