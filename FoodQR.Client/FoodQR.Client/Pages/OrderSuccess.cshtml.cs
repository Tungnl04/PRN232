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
            var baseUrl = config["ApiSettings:BaseUrl"];
            if (string.IsNullOrEmpty(baseUrl) || baseUrl.Contains("YOUR_API_DOMAIN"))
            {
                _apiBaseUrl = "https://foodqrrestaurant-cbdwbzfcfxecdfay.southeastasia-01.azurewebsites.net/api";
            }
            else
            {
                _apiBaseUrl = baseUrl;
            }
        }

        public string OrderCode { get; set; } = "";
        public int OrderId { get; set; }
        public int TableId { get; set; }
        public string? Token { get; set; }

        public async Task OnGetAsync(int orderId, int tableId, string? orderCode, string? token)
        {
            OrderId = orderId;
            TableId = tableId;
            Token = token;

            // Nếu đã có orderCode từ query string (truyền từ Cart page)
            if (!string.IsNullOrEmpty(orderCode))
            {
                OrderCode = orderCode;
                return;
            }

            // Fallback: gọi active endpoint (AllowAnonymous)
            if (tableId > 0)
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync($"{_apiBaseUrl}/Orders/active/{tableId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    OrderCode = doc.RootElement.GetProperty("orderCode").GetString() ?? "";
                }
            }
        }
    }
}
