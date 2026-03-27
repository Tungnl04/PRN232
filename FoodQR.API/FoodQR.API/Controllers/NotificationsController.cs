using FoodQR.API.Core.Entities;
using FoodQR.API.Hubs;
using FoodQR.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FoodQR.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly FoodStoreDbContext _context;
        private readonly IHubContext<OrderHub> _hubContext;

        public NotificationsController(FoodStoreDbContext context, IHubContext<OrderHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [Authorize]
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

        [Authorize(Roles = "staff,admin,kitchen")]
        [HttpPost]
        public async Task<IActionResult> CreateNotification([FromBody] Notification notification)
        {
            notification.IsRead = false;
            notification.CreatedAt = DateTime.Now;
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Broadcast qua SignalR
            var targetGroup = notification.TargetRole ?? "staff";
            await _hubContext.Clients.Group(targetGroup).SendAsync("NewNotification", new
            {
                id = notification.Id,
                message = notification.Message,
                type = notification.Type,
                targetRole = notification.TargetRole,
                createdAt = notification.CreatedAt
            });

            return Ok(new { id = notification.Id });
        }

        [AllowAnonymous]
        [HttpPost("call-staff")]
        public async Task<IActionResult> CallStaff([FromBody] Notification notification)
        {
            // Specifically for customer calling staff.
            notification.IsRead = false;
            notification.CreatedAt = DateTime.Now;
            notification.Type = "call_staff";
            notification.TargetRole = "staff"; // Ensure it targets staff
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group("staff").SendAsync("NewNotification", new
            {
                id = notification.Id,
                message = notification.Message,
                type = notification.Type,
                targetRole = notification.TargetRole,
                createdAt = notification.CreatedAt
            });

            // Also send to admin if they are listening to the same
            await _hubContext.Clients.Group("admin").SendAsync("NewNotification", new
            {
                id = notification.Id,
                message = notification.Message,
                type = notification.Type,
                targetRole = notification.TargetRole,
                createdAt = notification.CreatedAt
            });

            return Ok(new { id = notification.Id });
        }

        [Authorize]
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
