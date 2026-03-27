using FoodQR.API.Core.Entities;
using FoodQR.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OData.Query;

namespace FoodQR.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CouponsController : ControllerBase
    {
        private readonly FoodStoreDbContext _context;

        public CouponsController(FoodStoreDbContext context)
        {
            _context = context;
        }

        private static bool IsFixedType(string? discountType)
            => string.Equals(discountType, "fixed", StringComparison.OrdinalIgnoreCase);

        private static bool IsPercentType(string? discountType)
            => string.Equals(discountType, "percent", StringComparison.OrdinalIgnoreCase);

        private static string NormalizeDiscountType(string? discountType)
        {
            if (IsFixedType(discountType)) return "Fixed";
            if (IsPercentType(discountType)) return "Percent";
            return string.Empty;
        }

        private static string? ValidateCouponInput(Coupon coupon)
        {
            if (string.IsNullOrWhiteSpace(coupon.Code))
                return "Mã coupon không được để trống.";

            var normalizedType = NormalizeDiscountType(coupon.DiscountType);
            if (string.IsNullOrEmpty(normalizedType))
                return "Loại giảm giá không hợp lệ. Chỉ chấp nhận Percent hoặc Fixed.";

            if (coupon.DiscountValue < 0)
                return "Giá trị giảm không được âm.";

            if (coupon.MinOrderAmount < 0)
                return "Đơn tối thiểu không được âm.";

            if (coupon.MaxUsage.HasValue && coupon.MaxUsage.Value <= 0)
                return "Giới hạn số lượt dùng phải lớn hơn 0.";

            if (normalizedType == "Percent" && coupon.DiscountValue > 100)
                return "Mã giảm theo phần trăm chỉ được từ 0 đến 100.";

            coupon.DiscountType = normalizedType;
            return null;
        }

        [EnableQuery]
        [Authorize(Roles = "admin,staff")]
        [HttpGet]
        public IQueryable<Coupon> GetCoupons()
        {
            return _context.Coupons;
        }

        [Authorize(Roles = "admin,staff")]
        [HttpGet("{id}")]
        public async Task<ActionResult<Coupon>> GetCoupon(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null) return NotFound();
            return coupon;
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<ActionResult<Coupon>> CreateCoupon(Coupon coupon)
        {
            // Auto capitalize code
            coupon.Code = coupon.Code.ToUpperInvariant().Trim();
            var validationError = ValidateCouponInput(coupon);
            if (!string.IsNullOrEmpty(validationError))
                return BadRequest(new { Title = validationError });
            
            // Check duplicate
            if (await _context.Coupons.AnyAsync(c => c.Code == coupon.Code))
            {
                return BadRequest(new { Title = "Mã coupon đã tồn tại." });
            }

            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCoupon), new { id = coupon.Id }, coupon);
        }

        [Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCoupon(int id, Coupon coupon)
        {
            if (id != coupon.Id) return BadRequest();
            
            coupon.Code = coupon.Code.ToUpperInvariant().Trim();
            var validationError = ValidateCouponInput(coupon);
            if (!string.IsNullOrEmpty(validationError))
                return BadRequest(new { Title = validationError });
            _context.Entry(coupon).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCoupon(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null) return NotFound();

            coupon.IsActive = false;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        public class CouponValidateRequest
        {
            public string Code { get; set; } = null!;
            public decimal OrderTotal { get; set; }
        }

        [AllowAnonymous]
        [HttpPost("validate")]
        public async Task<IActionResult> ValidateCoupon([FromBody] CouponValidateRequest request)
        {
            var code = request.Code.ToUpperInvariant().Trim();
            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == code && c.IsActive);

            if (coupon == null) return BadRequest(new { Error = "Mã coupon không hợp lệ hoặc đã ngừng hoạt động." });
            
            if (coupon.ExpiryDate.HasValue && coupon.ExpiryDate.Value < DateTime.UtcNow)
                return BadRequest(new { Error = "Mã coupon đã hết hạn." });

            if (coupon.MaxUsage.HasValue && coupon.UsedCount >= coupon.MaxUsage.Value)
                return BadRequest(new { Error = "Mã coupon đã hết lượt sử dụng." });

            if (request.OrderTotal < coupon.MinOrderAmount)
                return BadRequest(new { Error = $"Đơn hàng cần tối thiểu {coupon.MinOrderAmount:N0} để áp mã." });

            decimal discount = 0;
            if (IsFixedType(coupon.DiscountType))
            {
                discount = coupon.DiscountValue;
            }
            else if (IsPercentType(coupon.DiscountType))
            {
                if (coupon.DiscountValue > 100)
                    return BadRequest(new { Error = "Mã giảm theo phần trăm không hợp lệ (vượt quá 100%)." });
                discount = request.OrderTotal * (coupon.DiscountValue / 100m);
            }
            else
            {
                return BadRequest(new { Error = "Loại mã giảm không hợp lệ." });
            }

            // Optional cap if we wanted max_discount field, ignoring for now
            if (discount > request.OrderTotal) discount = request.OrderTotal;

            return Ok(new 
            {
                CouponId = coupon.Id,
                Code = coupon.Code,
                DiscountAmount = discount
            });
        }
    }
}
