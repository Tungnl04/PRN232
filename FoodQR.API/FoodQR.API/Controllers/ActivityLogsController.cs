using FoodQR.API.Core.Entities;
using FoodQR.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace FoodQR.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "admin")]
    public class ActivityLogsController : ControllerBase
    {
        private readonly FoodStoreDbContext _context;

        public ActivityLogsController(FoodStoreDbContext context)
        {
            _context = context;
        }

        [EnableQuery]
        [HttpGet]
        public IQueryable<object> GetActivityLogs()
        {
            return _context.ActivityLogs
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new
                {
                    a.Id,
                    a.UserId,
                    UserName = a.UserId != null 
                        ? _context.Users.Where(u => u.Id == a.UserId).Select(u => u.Name).FirstOrDefault() 
                        : "System",
                    a.Action,
                    a.Description,
                    a.CreatedAt
                });
        }
    }
}
