using FoodQR.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodQR.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly FoodStoreDbContext _context;

        public NotificationsController(FoodStoreDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Notification>>> GetNotifications(string? role)
        {
            IQueryable<Notification> query = _context.Notifications;
            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(n => n.TargetRole == role);
            }
            return await query.OrderByDescending(n => n.CreatedAt).Take(20).ToListAsync();
        }

        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) return NotFound();

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
