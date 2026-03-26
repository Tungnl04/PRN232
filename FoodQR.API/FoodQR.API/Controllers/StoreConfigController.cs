using FoodQR.API.Core.Entities;
using FoodQR.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodQR.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StoreConfigController : ControllerBase
    {
        private readonly FoodStoreDbContext _context;

        public StoreConfigController(FoodStoreDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy cấu hình cửa hàng (thuế, tên, v.v.)
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetConfig()
        {
            var config = await _context.StoreConfigurations.FirstOrDefaultAsync();
            if (config == null)
            {
                // Auto-create default config if none exists
                config = new StoreConfiguration();
                _context.StoreConfigurations.Add(config);
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                config.Id,
                config.StoreName,
                config.TaxRate,
                config.IsTaxIncludedInPrice,
                config.Currency,
                config.UpdatedAt
            });
        }

        /// <summary>
        /// Cập nhật cấu hình cửa hàng (chỉ Admin)
        /// </summary>
        [HttpPut]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateConfig([FromBody] UpdateStoreConfigDto dto)
        {
            var config = await _context.StoreConfigurations.FirstOrDefaultAsync();
            if (config == null)
            {
                config = new StoreConfiguration();
                _context.StoreConfigurations.Add(config);
            }

            if (dto.StoreName != null) config.StoreName = dto.StoreName;
            if (dto.TaxRate.HasValue)
            {
                if (dto.TaxRate.Value < 0 || dto.TaxRate.Value > 1)
                    return BadRequest(new { Error = "TaxRate phải nằm trong khoảng 0 đến 1 (VD: 0.08 = 8%)" });
                config.TaxRate = dto.TaxRate.Value;
            }
            if (dto.IsTaxIncludedInPrice.HasValue) config.IsTaxIncludedInPrice = dto.IsTaxIncludedInPrice.Value;
            if (dto.Currency != null) config.Currency = dto.Currency;
            config.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                config.Id,
                config.StoreName,
                config.TaxRate,
                config.IsTaxIncludedInPrice,
                config.Currency,
                config.UpdatedAt
            });
        }
    }

    public class UpdateStoreConfigDto
    {
        public string? StoreName { get; set; }
        public decimal? TaxRate { get; set; }
        public bool? IsTaxIncludedInPrice { get; set; }
        public string? Currency { get; set; }
    }
}
