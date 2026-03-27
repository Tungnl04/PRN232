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
            
            // Check duplicate
            if (await _context.Coupons.AnyAsync(c => c.Code == coupon.Code))
            {
                return BadRequest(new { Title = "Coupon code already exists!" });
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

            if (coupon == null) return BadRequest(new { Error = "Invalid or inactive coupon code." });
            
            if (coupon.ExpiryDate.HasValue && coupon.ExpiryDate.Value < DateTime.UtcNow)
                return BadRequest(new { Error = "Coupon has expired." });

            if (coupon.MaxUsage.HasValue && coupon.UsedCount >= coupon.MaxUsage.Value)
                return BadRequest(new { Error = "Coupon usage limit reached." });

            if (request.OrderTotal < coupon.MinOrderAmount)
                return BadRequest(new { Error = $"Minimum order amount of {coupon.MinOrderAmount:N0} required." });

            decimal discount = 0;
            if (string.Equals(coupon.DiscountType, "fixed", StringComparison.OrdinalIgnoreCase) || string.Equals(coupon.DiscountType, "Fixed", StringComparison.OrdinalIgnoreCase))
            {
                discount = coupon.DiscountValue;
            }
            else if (string.Equals(coupon.DiscountType, "percent", StringComparison.OrdinalIgnoreCase) || string.Equals(coupon.DiscountType, "Percent", StringComparison.OrdinalIgnoreCase))
            {
                discount = request.OrderTotal * (coupon.DiscountValue / 100m);
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
