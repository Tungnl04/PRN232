using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodQR.Client.Pages
{
    public class NotFoundModel : PageModel
    {
        public void OnGet()
        {
            Response.StatusCode = 404;
        }
    }
}
