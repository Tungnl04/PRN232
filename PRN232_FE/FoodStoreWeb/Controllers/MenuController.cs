using FoodStoreWeb.Interfaces;
using Microsoft.AspNetCore.Mvc;
using FoodStoreWeb.Services;

namespace FoodStoreWeb.Controllers
{
    public class MenuController : Controller
    {
        private readonly IApiClient _apiClient;

        public MenuController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _apiClient.GetCategoriesWithProductsAsync();
            var combos = await _apiClient.GetCombosAsync();

            ViewBag.Combos = combos;
            ViewBag.Categories = categories;
            return View();
        }
    }
}