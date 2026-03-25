using FoodQR.API.Application.DTOs;
using FoodQR.API.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodQR.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "kitchen,admin,staff")]
    public class KitchenController : ControllerBase
    {
        private readonly IKitchenService _kitchenService;

        public KitchenController(IKitchenService kitchenService)
        {
            _kitchenService = kitchenService;
        }

        [HttpGet("items")]
        public async Task<ActionResult<IEnumerable<KitchenItemDto>>> GetKitchenItems()
        {
            var items = await _kitchenService.GetKitchenItemsAsync();
            return Ok(items);
        }

        [HttpPatch("items/{id}/status")]
        public async Task<IActionResult> UpdateItemStatus(int id, [FromBody] string status)
        {
            var result = await _kitchenService.UpdateItemStatusAsync(id, status);
            if (!result) return BadRequest("Unable to update item status. Note: Only pending items can be cancelled, and statuses must be valid.");
            
            return NoContent();
        }
    }
}
