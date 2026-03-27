using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoodQR.API.Core.Entities;

public partial class Category
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên Danh mục không được để trống")]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool? IsAvailable { get; set; } = true;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
