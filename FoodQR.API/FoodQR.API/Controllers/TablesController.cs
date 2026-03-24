using FoodQR.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodQR.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TablesController : ControllerBase
    {
        private readonly FoodStoreDbContext _context;

        public TablesController(FoodStoreDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderTable>>> GetTables()
        {
            return await _context.OrderTables.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OrderTable>> GetTable(int id)
        {
            var table = await _context.OrderTables.FindAsync(id);
            if (table == null) return NotFound();
            return table;
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateTableStatus(int id, [FromBody] string status)
        {
            var table = await _context.OrderTables.FindAsync(id);
            if (table == null) return NotFound();

            table.Status = status;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
