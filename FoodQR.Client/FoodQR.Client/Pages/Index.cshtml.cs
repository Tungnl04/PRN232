using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace FoodQR.Client.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<IndexModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            
            var baseUrl = config["ApiSettings:BaseUrl"];
            // SEC-FIX: Nếu là placeholder thì dùng link production thật
            if (string.IsNullOrEmpty(baseUrl) || baseUrl.Contains("YOUR_API_DOMAIN"))
            {
                _apiBaseUrl = "https://foodqrrestaurant-cbdwbzfcfxecdfay.southeastasia-01.azurewebsites.net/api";
            }
            else
            {
                _apiBaseUrl = baseUrl;
            }
        }

        public List<CategoryDto> Categories { get; set; } = new();
        public List<ProductDto> Products { get; set; } = new();
        public List<ComboDto> Combos { get; set; } = new();
        public int TableId { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync(int? tableId)
        {
            TableId = tableId ?? 0;
            
            var client = _httpClientFactory.CreateClient();
            
            try 
            {
                // Fetch Categories
                var catResponse = await client.GetAsync($"{_apiBaseUrl}/Categories");
                if (catResponse.IsSuccessStatusCode)
                {
                    var catJson = await catResponse.Content.ReadAsStringAsync();
                    Categories = JsonSerializer.Deserialize<List<CategoryDto>>(catJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }

                var prodResponse = await client.GetAsync($"{_apiBaseUrl}/Products");
                if (prodResponse.IsSuccessStatusCode)
                {
                    var prodJson = await prodResponse.Content.ReadAsStringAsync();
                    Products = JsonSerializer.Deserialize<List<ProductDto>>(prodJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
                else
                {
                    ErrorMessage = $"Lỗi tải Món Ăn: HTTP {(int)prodResponse.StatusCode} ({prodResponse.StatusCode}) - URL: {_apiBaseUrl}/Products";
                }

                // Fetch Combos
                var comboResponse = await client.GetAsync($"{_apiBaseUrl}/Combos");
                if (comboResponse.IsSuccessStatusCode)
                {
                    var comboJson = await comboResponse.Content.ReadAsStringAsync();
                    Combos = JsonSerializer.Deserialize<List<ComboDto>>(comboJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch menu data from API");
                ErrorMessage = $"Lỗi kết nối từ Client Server tới API: {ex.Message} (URL: {_apiBaseUrl}/Products)";
            }
        }
    }

    public class CategoryDto { public int Id { get; set; } public string Name { get; set; } = null!; }
    public class ProductDto { public int Id { get; set; } public string Name { get; set; } = null!; public string? Description { get; set; } public decimal Price { get; set; } public string? ImageUrl { get; set; } public int CategoryId { get; set; } }
    public class ComboDto { public int Id { get; set; } public string Name { get; set; } = null!; public string? Description { get; set; } public decimal Price { get; set; } public string? ImageUrl { get; set; } public bool IsAvailable { get; set; } }
}
