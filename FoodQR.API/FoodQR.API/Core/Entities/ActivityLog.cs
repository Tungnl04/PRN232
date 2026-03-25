using System;
using System.Collections.Generic;

namespace FoodQR.API.Core.Entities;

public partial class ActivityLog
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string Action { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }
}
