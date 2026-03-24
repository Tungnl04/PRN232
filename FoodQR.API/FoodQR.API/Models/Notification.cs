using System;
using System.Collections.Generic;

namespace FoodQR.API.Models;

public partial class Notification
{
    public int Id { get; set; }

    public string Message { get; set; } = null!;

    public string? Type { get; set; }

    public string? TargetRole { get; set; }

    public bool? IsRead { get; set; }

    public DateTime? CreatedAt { get; set; }
}
