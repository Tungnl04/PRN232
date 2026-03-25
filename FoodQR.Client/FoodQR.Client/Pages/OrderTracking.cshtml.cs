using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace FoodQR.Client.Pages
{
    public class OrderTrackingModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl = "https://localhost:7197/api";

        public OrderTrackingModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public int OrderId { get; set; }
        public int TableId { get; set; }
        public string OrderCode { get; set; } = "";
        public string OrderStatus { get; set; } = "";
        public List<TrackingItemDto> Items { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int orderId, int tableId)
        {
            OrderId = orderId;
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{_apiBaseUrl}/Orders/{orderId}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                
                int orderTableId = root.GetProperty("tableId").GetInt32();
                
                // Security check: Đơn hàng phải thuộc về bàn này
                if (orderTableId != tableId)
                {
                    return RedirectToPage("/Index", new { tableId = tableId });
                }

                OrderCode = root.GetProperty("orderCode").GetString() ?? "";
                OrderStatus = root.GetProperty("status").GetString() ?? "";
                TableId = orderTableId;

                var itemsArray = root.GetProperty("orderItems");
                foreach (var item in itemsArray.EnumerateArray())
                {
                    string itemName = "";
                    if (item.TryGetProperty("productId", out var pid2) && pid2.ValueKind != JsonValueKind.Null)
                    {
                         var pResponse = await client.GetAsync($"{_apiBaseUrl}/Products/{pid2.GetInt32()}");
                         if (pResponse.IsSuccessStatusCode) {
                             var pJson = await pResponse.Content.ReadAsStringAsync();
                             using var pDoc = JsonDocument.Parse(pJson);
                             itemName = pDoc.RootElement.GetProperty("name").GetString() ?? "";
                         }
                    }
                    else if (item.TryGetProperty("comboId", out var cid) && cid.ValueKind != JsonValueKind.Null)
                    {
                         var cResponse = await client.GetAsync($"{_apiBaseUrl}/Combos/{cid.GetInt32()}");
                         if (cResponse.IsSuccessStatusCode) {
                             var cJson = await cResponse.Content.ReadAsStringAsync();
                             using var cDoc = JsonDocument.Parse(cJson);
                             itemName = cDoc.RootElement.GetProperty("name").GetString() ?? "";
                         }
                    }

                    if (string.IsNullOrEmpty(itemName)) itemName = "Unknown Item";

                    Items.Add(new TrackingItemDto {
                        Name = itemName,
                        Quantity = item.GetProperty("quantity").GetInt32(),
                        Status = item.GetProperty("status").GetString() ?? "Pending"
                    });
                }
                return Page();
            }
            return RedirectToPage("/Index", new { tableId = tableId });
        }
    }

    public class TrackingItemDto { public string Name { get; set; } = ""; public int Quantity { get; set; } public string Status { get; set; } = ""; }
}
