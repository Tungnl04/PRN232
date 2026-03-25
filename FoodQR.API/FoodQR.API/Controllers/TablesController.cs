using FoodQR.API.Core.Entities;
using FoodQR.API.Infrastructure.Persistence;
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

        [HttpPost]
        public async Task<ActionResult<OrderTable>> PostTable(OrderTable table)
        {
            _context.OrderTables.Add(table);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetTable), new { id = table.Id }, table);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutTable(int id, OrderTable table)
        {
            if (id != table.Id) return BadRequest();
            _context.Entry(table).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTable(int id)
        {
            var table = await _context.OrderTables.FindAsync(id);
            if (table == null) return NotFound();
            _context.OrderTables.Remove(table);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
