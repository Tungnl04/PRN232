using System;
using System.Collections.Generic;

namespace FoodQR.API.Models;

public partial class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    public int? Inventory { get; set; }

    public int? CategoryId { get; set; }

    public bool? IsAvailable { get; set; }

    public virtual Category? Category { get; set; }

    public virtual ICollection<ComboItem> ComboItems { get; set; } = new List<ComboItem>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
