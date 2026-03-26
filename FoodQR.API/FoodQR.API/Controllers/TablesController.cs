using FoodQR.API.Application.DTOs;
using FoodQR.API.Core.Entities;
using FoodQR.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
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

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TableResponseDto>>> GetTables()
        {
            var tables = await _context.OrderTables.ToListAsync();
            return tables.Select(t => new TableResponseDto
            {
                Id = t.Id,
                TableNumber = t.TableNumber,
                Capacity = t.Capacity,
                Status = t.Status,
                QrCodeToken = t.QrCodeToken,
                Location = t.Location
            }).ToList();
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<TableResponseDto>> GetTable(int id)
        {
            var t = await _context.OrderTables.FindAsync(id);
            if (t == null) return NotFound();
            return new TableResponseDto
            {
                Id = t.Id,
                TableNumber = t.TableNumber,
                Capacity = t.Capacity,
                Status = t.Status,
                QrCodeToken = t.QrCodeToken,
                Location = t.Location
            };
        }

        [AllowAnonymous]
        [HttpGet("by-token/{token}")]
        public async Task<ActionResult<TableResponseDto>> GetTableByToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(new { message = "Token không hợp lệ." });

            var table = await _context.OrderTables
                .FirstOrDefaultAsync(t => t.QrCodeToken == token);

            if (table == null)
                return NotFound(new { message = "QR code không hợp lệ hoặc đã hết hạn." });

            return Ok(new TableResponseDto
            {
                Id = table.Id,
                TableNumber = table.TableNumber,
                Capacity = table.Capacity,
                Status = table.Status,
                QrCodeToken = table.QrCodeToken,
                Location = table.Location
            });
        }


        [Authorize(Roles = "staff,admin")]
        [HttpPost("{id}/generate-qr")]
        public async Task<ActionResult> GenerateQrToken(int id)
        {
            var table = await _context.OrderTables.FindAsync(id);
            if (table == null) return NotFound(new { message = "Không tìm thấy bàn." });

            table.QrCodeToken = Guid.NewGuid().ToString("N");

            await _context.SaveChangesAsync();

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var orderUrl = $"{baseUrl}/?token={table.QrCodeToken}";

            return Ok(new
            {
                tableId = table.Id,
                tableNumber = table.TableNumber,
                token = table.QrCodeToken,
                orderUrl   
            });
        }

        [Authorize(Roles = "staff,admin")]
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateTableStatus(int id, [FromBody] string status)
        {
            var table = await _context.OrderTables.FindAsync(id);
            if (table == null) return NotFound();

            table.Status = status;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<ActionResult<OrderTable>> PostTable(OrderTable table)
        {
            table.QrCodeToken = Guid.NewGuid().ToString("N");

            _context.OrderTables.Add(table);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetTable), new { id = table.Id }, table);
        }

        [Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTable(int id, OrderTable table)
        {
            if (id != table.Id) return BadRequest();
            _context.Entry(table).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [Authorize(Roles = "admin")]
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
