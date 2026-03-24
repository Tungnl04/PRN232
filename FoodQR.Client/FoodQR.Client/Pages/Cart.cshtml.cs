using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodQR.Client.Pages
{
    public class CartModel : PageModel
    {
        public int TableId { get; set; }

        public void OnGet(int? tableId)
        {
            TableId = tableId ?? 0;
        }
    }
}
