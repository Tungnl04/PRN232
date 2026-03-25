using System;
using System.Collections.Generic;

namespace FoodQR.API.Core.Entities;

public partial class User
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? Role { get; set; }

    public bool? Active { get; set; }
}
