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
    [Authorize(Roles = "admin")]
    public class UsersController : ControllerBase
    {
        private readonly FoodStoreDbContext _context;

        public UsersController(FoodStoreDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsers()
        {
            var users = await _context.Users.ToListAsync();
            return users.Select(u => new UserResponseDto { Id = u.Id, Name = u.Name, Username = u.Username, Role = u.Role, Active = u.Active }).ToList();
        }

        [HttpPost]
        public async Task<ActionResult<UserResponseDto>> CreateUser(User user)
        {
            if (_context.Users.Any(u => u.Username == user.Username))
                return BadRequest("Username already taken.");

            // Hash the password using BCrypt
            if (!string.IsNullOrEmpty(user.PasswordHash))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            }

            user.Active = true;
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, new UserResponseDto { Id = user.Id, Name = user.Name, Username = user.Username, Role = user.Role, Active = user.Active });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UpdateUserDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Update only provided fields
            if (!string.IsNullOrEmpty(dto.Name)) user.Name = dto.Name;
            if (!string.IsNullOrEmpty(dto.Role)) user.Role = dto.Role;
            if (dto.Active.HasValue) user.Active = dto.Active.Value;

            // Hash password if a new one is provided
            if (!string.IsNullOrEmpty(dto.NewPassword))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            
            user.Active = false; // Soft delete
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
