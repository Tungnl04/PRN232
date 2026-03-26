using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace FoodQR.Client.Pages
{
    public class OrderTrackingModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl;

        public OrderTrackingModel(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _apiBaseUrl = config["ApiSettings:BaseUrl"] ?? "https://localhost:7197/api";
        }

        public int OrderId { get; set; }
        public int TableId { get; set; }
        public string OrderCode { get; set; } = "";
        public string OrderStatus { get; set; } = "";
        public string? Token { get; set; }
        public List<TrackingItemDto> Items { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int orderId, int tableId, string? token)
        {
            OrderId = orderId;
            TableId = tableId;
            Token = token;
            var client = _httpClientFactory.CreateClient();

            // BUG-09 Fix: Use active/{tableId} endpoint which returns items with names pre-loaded
            // This eliminates N+1 HTTP calls (previously 1 call per item to get product/combo name)
            var response = await client.GetAsync($"{_apiBaseUrl}/Orders/active/{tableId}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Verify this is the correct order
                int returnedOrderId = root.GetProperty("id").GetInt32();
                if (returnedOrderId != orderId)
                {
                    if (!string.IsNullOrEmpty(Token)) return RedirectToPage("/Index", new { token = Token });
                    return RedirectToPage("/Index", new { tableId = tableId });
                }

                OrderCode = root.GetProperty("orderCode").GetString() ?? "";
                OrderStatus = root.GetProperty("status").GetString() ?? "";

                // Items already contain names from the API response - no N+1!
                var itemsArray = root.GetProperty("items");
                foreach (var item in itemsArray.EnumerateArray())
                {
                    Items.Add(new TrackingItemDto
                    {
                        Name = item.GetProperty("name").GetString() ?? "Unknown Item",
                        Quantity = item.GetProperty("quantity").GetInt32(),
                        Status = item.GetProperty("status").GetString() ?? "Pending",
                        Note = item.TryGetProperty("note", out var noteProp) ? noteProp.GetString() : null
                    });
                }
                return Page();
            }
            if (!string.IsNullOrEmpty(Token)) return RedirectToPage("/Index", new { token = Token });
            return RedirectToPage("/Index", new { tableId = tableId });
        }
    }

    public class TrackingItemDto 
    { 
        public string Name { get; set; } = ""; 
        public int Quantity { get; set; } 
        public string Status { get; set; } = ""; 
        public string? Note { get; set; }
    }
}
