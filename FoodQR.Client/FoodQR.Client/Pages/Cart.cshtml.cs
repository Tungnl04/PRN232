using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodQR.Client.Pages
{
    public class CartModel : PageModel
    {
        private readonly IConfiguration _config;

        public CartModel(IConfiguration config)
        {
            _config = config;
        }

        public int TableId { get; set; }

        public void OnGet(int? tableId)
        {
            TableId = tableId ?? 0;
            var baseUrl = _config["ApiSettings:BaseUrl"];
            if (string.IsNullOrEmpty(baseUrl) || baseUrl.Contains("YOUR_API_DOMAIN"))
            {
                ViewData["ApiBaseUrl"] = "https://foodqrrestaurant-cbdwbzfcfxecdfay.southeastasia-01.azurewebsites.net/api";
            }
            else
            {
                ViewData["ApiBaseUrl"] = baseUrl;
            }
        }
    }
}
