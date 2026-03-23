using FoodStoreWeb.DTOs;

namespace FoodStoreWeb.Interfaces
{
    public interface IApiClient
    {
        // Menu
        Task<List<CategoryDto>> GetCategoriesWithProductsAsync();
        Task<List<ComboDto>> GetCombosAsync();

        // Auth
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
        Task<UserDto?> GetUserByIdAsync(decimal id);
        Task<bool> UpdateUserAsync(decimal id, UpdateUserDto dto);
        Task<bool> ChangePasswordAsync(decimal id, ChangePasswordDto dto);
    }
}
