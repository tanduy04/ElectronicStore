using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        private int GetAccountId()
        {
            return int.Parse(User.FindFirst("AccountID").Value);
        }
        // Get user's cart
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var accountId = GetAccountId();
            var result = await _cartService.GetCartByAccountIdAsync(accountId);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }

        // Add product to cart
        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            var accountId = GetAccountId();
            var result = await _cartService.AddToCartAsync(accountId, dto);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(new { Message = result.Message });
        }


        // Update product quantity
        [HttpPut("update")]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateCartDto dto)
        {
            var accountId = GetAccountId();
            var result = await _cartService.UpdateCartItemAsync(accountId, dto.ProductId, dto.Quantity);

            if (!result.Success)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        // Remove product from cart
        [HttpDelete("remove/{productId}")]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            var accountId = GetAccountId();
            var result = await _cartService.RemoveFromCartAsync(accountId, productId);

            if (!result.Success)
                return NotFound(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        // Clear cart
        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            var accountId = GetAccountId();
            var result = await _cartService.ClearCartAsync(accountId);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(new { Message = result.Message });
        }
    }
}
