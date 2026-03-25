using FoodQR.API.Core.Entities;
using FoodQR.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodQR.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CombosController : ControllerBase
    {
        private readonly FoodStoreDbContext _context;

        public CombosController(FoodStoreDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetCombos()
        {
            // Fetch combos and their items to check availability
            var combos = await _context.Combos
                .Include(c => c.ComboItems)
                    .ThenInclude(ci => ci.Product)
                .ToListAsync();

            var result = combos.Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                c.Price,
                c.ImageUrl,
                // Business Rule: Combo disable nếu món con hết hàng
                IsAvailable = c.Available == true && c.ComboItems.All(ci => ci.Product != null && ci.Product.IsAvailable == true)
            }).Where(c => c.IsAvailable) // Hide unavailable combos for guests
            .ToList();

            return result;
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<Combo>> GetCombo(int id)
        {
            var combo = await _context.Combos
                .Include(c => c.ComboItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (combo == null) return NotFound();
            return combo;
        }
    }
}
