using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace FoodQR.Client.Pages
{
    public class OrderSuccessModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl;

        public OrderSuccessModel(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _apiBaseUrl = config["ApiSettings:BaseUrl"] ?? "https://localhost:7197/api";
        }

        public string OrderCode { get; set; } = "";
        public int OrderId { get; set; }
        public int TableId { get; set; }

        public async Task OnGetAsync(int orderId)
        {
            OrderId = orderId;
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{_apiBaseUrl}/Orders/{orderId}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                OrderCode = doc.RootElement.GetProperty("orderCode").GetString() ?? "";
                TableId = doc.RootElement.GetProperty("tableId").GetInt32();
            }
        }
    }
}
