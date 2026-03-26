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
        public async Task<ActionResult<IEnumerable<dynamic>>> GetCombos([FromQuery] bool includeHidden = false)
        {
            var combos = await _context.Combos
                .Include(c => c.ComboItems)
                    .ThenInclude(ci => ci.Product)
                .ToListAsync();

            var resultQuery = combos.Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                c.Price,
                c.ImageUrl,
                IsAvailable = c.Available == true && c.ComboItems.All(ci => ci.Product != null && ci.Product.IsAvailable == true),
                ComboItems = c.ComboItems.Select(ci => new { 
                    ci.ProductId, 
                    ci.Quantity,
                    Product = new { Name = ci.Product?.Name }
                })
            });

            if (!includeHidden)
            {
                return resultQuery.Where(c => c.IsAvailable).ToList();
            }

            return resultQuery.ToList();
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

        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<ActionResult<Combo>> CreateCombo(FoodQR.API.Application.DTOs.ComboCreateDto comboDto)
        {
            if (comboDto.Items == null || !comboDto.Items.Any()) return BadRequest("Combo must have at least one product.");

            var combo = new Combo
            {
                Name = comboDto.Name,
                Description = comboDto.Description,
                Price = comboDto.Price,
                ImageUrl = comboDto.ImageUrl,
                Available = comboDto.Available
            };

            foreach (var item in comboDto.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null) return BadRequest($"Product '{item.ProductId}' not found.");

                combo.ComboItems.Add(new ComboItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                });
            }

            _context.Combos.Add(combo);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCombo), new { id = combo.Id }, combo);
        }

        [Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCombo(int id, FoodQR.API.Application.DTOs.ComboCreateDto comboDto)
        {
            var combo = await _context.Combos
                .Include(c => c.ComboItems)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (combo == null) return NotFound();

            combo.Name = comboDto.Name;
            combo.Description = comboDto.Description;
            combo.Price = comboDto.Price;
            combo.ImageUrl = comboDto.ImageUrl;
            combo.Available = comboDto.Available;

            // Remove old items
            _context.ComboItems.RemoveRange(combo.ComboItems);
            combo.ComboItems.Clear();

            // Add new items
            foreach (var item in comboDto.Items)
            {
                 combo.ComboItems.Add(new ComboItem
                 {
                     ProductId = item.ProductId,
                     Quantity = item.Quantity
                 });
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCombo(int id)
        {
            var combo = await _context.Combos.FindAsync(id);
            if (combo == null) return NotFound();

            combo.Available = false; // Soft delete or unavailable
            await _context.SaveChangesAsync();
            
            return NoContent();
        }
    }
}
