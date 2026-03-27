namespace FoodQR.API.Core.Entities;

public class StoreConfiguration
{
    public int Id { get; set; }
    public string StoreName { get; set; } = "FoodQR Restaurant";
    public decimal TaxRate { get; set; } = 0.08m;
    public bool IsTaxIncludedInPrice { get; set; } = false;
    public string Currency { get; set; } = "VND";
    public DateTime? UpdatedAt { get; set; }
}
