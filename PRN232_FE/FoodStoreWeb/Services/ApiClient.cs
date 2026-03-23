using System.Net.Http.Json;
using System.Text.Json;
using FoodStoreWeb.DTOs;
using FoodStoreWeb.Interfaces;

namespace FoodStoreWeb.Services
{
    public class ApiClient : IApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiClient> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ApiClient(HttpClient httpClient, ILogger<ApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        private async Task<T?> UnwrapDataAsync<T>(HttpResponseMessage response)
        {
            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiResponse<T>>(_jsonOptions);
            return wrapper != null ? wrapper.Data : default;
        }

        // Get data for menu (categories & products)

        public async Task<List<CategoryDto>> GetCategoriesWithProductsAsync()
        {
            try
            {
                var catResponse = await _httpClient.GetAsync("AdminApi/categories");
                var prodResponse = await _httpClient.GetAsync("AdminApi/products");
                if (!catResponse.IsSuccessStatusCode) return new List<CategoryDto>();

                var categories = (await UnwrapDataAsync<List<CategoryDto>>(catResponse)
                   ?? new List<CategoryDto>())
                   .OrderBy(c => c.Id)
                   .ToList();

                if (prodResponse.IsSuccessStatusCode)
                {
                    var products = await UnwrapDataAsync<List<ProductDto>>(prodResponse) ?? new List<ProductDto>();

                    foreach (var category in categories)
                    {
                        category.Products = products
                            .Where(p => p.CategoryId == category.Id)
                            .OrderBy(p => p.Id)
                            .ToList();
                    }
                }

                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách categories");
                return new List<CategoryDto>();
            }
        }

        public async Task<List<ComboDto>> GetCombosAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("AdminApi/combos");
                if (!response.IsSuccessStatusCode) return new List<ComboDto>();
                return await UnwrapDataAsync<List<ComboDto>>(response) ?? new List<ComboDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách combos");
                return new List<ComboDto>();
            }
        }

        // Login

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("AdminApi/auth/login", request);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<LoginResponseDto>(_jsonOptions)
                           ?? new LoginResponseDto { Success = false };

                return new LoginResponseDto { Success = false, Message = "Tên đăng nhập hoặc mật khẩu không đúng." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đăng nhập");
                return new LoginResponseDto { Success = false, Message = "Lỗi kết nối đến server." };
            }
        }

        public async Task<UserDto?> GetUserByIdAsync(decimal id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"AdminApi/users/{id}");
                if (!response.IsSuccessStatusCode) return null;
                return await UnwrapDataAsync<UserDto>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin user {Id}", id);
                return null;
            }
        }

        public async Task<bool> UpdateUserAsync(decimal id, UpdateUserDto dto)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"AdminApi/users/{id}", dto);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật user {Id}", id);
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(decimal id, ChangePasswordDto dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"AdminApi/users/{id}/change-password", dto);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đổi mật khẩu user {Id}", id);
                return false;
            }
        }

        //Orders

        public async Task<CreateOrderResponseDto> CreateOrderAsync(CreateOrderDto request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("OrderApi/orders", request);
                return await response.Content.ReadFromJsonAsync<CreateOrderResponseDto>(_jsonOptions)
                       ?? new CreateOrderResponseDto { Success = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo đơn hàng");
                return new CreateOrderResponseDto { Success = false, Message = "Lỗi kết nối đến server." };
            }
        }

        public async Task<OrdersByCodeResponseDto> GetOrdersByCodeAsync(string orderCode)
        {
            try
            {
                var response = await _httpClient.GetAsync($"OrderApi/orders/{orderCode}");
                if (!response.IsSuccessStatusCode)
                    return new OrdersByCodeResponseDto { Success = false, Message = "Không tìm thấy đơn hàng." };

                return await response.Content.ReadFromJsonAsync<OrdersByCodeResponseDto>(_jsonOptions)
                       ?? new OrdersByCodeResponseDto { Success = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tìm đơn hàng theo mã");
                return new OrdersByCodeResponseDto { Success = false, Message = "Lỗi kết nối đến server." };
            }
        }

        // Staff get all orders

        public async Task<List<OrderDto>> GetAllOrdersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("StaffApi/orders");
                if (!response.IsSuccessStatusCode) return new List<OrderDto>();
                return await UnwrapDataAsync<List<OrderDto>>(response) ?? new List<OrderDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách orders");
                return new List<OrderDto>();
            }
        }

        public async Task<List<OrderDto>> GetOrdersByStatusAsync(string status)
        {
            try
            {
                var response = await _httpClient.GetAsync($"StaffApi/orders/{status}");
                if (!response.IsSuccessStatusCode) return new List<OrderDto>();
                return await UnwrapDataAsync<List<OrderDto>>(response) ?? new List<OrderDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy orders theo status");
                return new List<OrderDto>();
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(decimal id, string status)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync(
                    $"StaffApi/order/{id}/status",
                    new { status });
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật status đơn hàng {Id}", id);
                return false;
            }
        }
    }

    // Wrapper để deserialize { success, data, message, count }
    internal class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public int? Count { get; set; }
    }
}