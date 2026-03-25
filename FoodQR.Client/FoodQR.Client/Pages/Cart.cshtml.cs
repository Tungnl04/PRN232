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
            ViewData["ApiBaseUrl"] = _config["ApiSettings:BaseUrl"] ?? "https://localhost:7197/api";
        }
    }
}
