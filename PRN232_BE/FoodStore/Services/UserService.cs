using Microsoft.EntityFrameworkCore;
using FoodStoreAPI.Interfaces;
using FoodStoreRepository.Models;

namespace TFC.Services
{
    public class UserService : IUserService
    {
        private readonly FoodStoreDbContext _context;

        public UserService(FoodStoreDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetUserByIdAsync(decimal id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<bool> ValidateUserAsync(string username, string password)
        {
            var user = await GetUserByUsernameAsync(username);
            if (user == null) return false;
            return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users.Where(u => u.Active.Equals(1)).ToListAsync();
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}