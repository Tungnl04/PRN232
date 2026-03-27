using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoodQR.API.Core.Entities;

public partial class Product
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên Sản phẩm không được để trống")]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    [Range(0, 50000000, ErrorMessage = "Giá phải từ 0 đến 50 triệu")]
    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Số lượng kho không hợp lệ")]
    public int? Inventory { get; set; }

    public int? CategoryId { get; set; }

    public bool? IsAvailable { get; set; }

    public virtual Category? Category { get; set; }

    public virtual ICollection<ComboItem> ComboItems { get; set; } = new List<ComboItem>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
