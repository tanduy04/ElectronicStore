using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ElectronicStoreContext _context;

        public CartController(ElectronicStoreContext context)
        {
            _context = context;
        }

        // Get AccountId from token
        private int GetAccountId()
        {
            return int.Parse(User.FindFirst("AccountID").Value);
        }

        // Get user's cart
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            int accountId = GetAccountId();

            var cart = await _context.Carts
                .Include(c => c.Product)
                .Where(c => c.CartId == accountId)
                .ToListAsync();

            if (!cart.Any())
                return Ok(new { Message = "Your cart is empty." });

            var result = cart.Select(c => new
            {
                c.CartId,
                c.ProductId,
                c.Product.ProductName,
                c.Product.Price,
                c.Quantity
            });

            return Ok(result);
        }

        // Add product to cart
        [HttpPost("add")]
        [Authorize]

        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            var productExists = await _context.Products.AnyAsync(p => p.ProductId == dto.ProductId);
            if (!productExists)
            {
                return BadRequest("Product not found");
            }
            int accountId = GetAccountId();

            int quantity = dto.Quantity;

            var cartItem = await _context.Carts
                .FirstOrDefaultAsync(c => c.CartId == accountId && c.ProductId == dto.ProductId);

            if (cartItem == null)
            {
                cartItem = new Cart
                {
                    CartId = accountId,
                    ProductId = dto.ProductId,
                    Quantity = quantity
                };
                _context.Carts.Add(cartItem);
            }
            else
            {
                cartItem.Quantity += quantity;
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Product added to cart successfully." });
        }


        // Update product quantity
        [HttpPut("update")]
        [Authorize]

        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateCartDto dto)
        {
            int accountId = GetAccountId();

            var cartItem = await _context.Carts
                .FirstOrDefaultAsync(c => c.CartId == accountId && c.ProductId == dto.ProductId);

            if (cartItem == null)
                return NotFound(new { Message = "Product not found in your cart." });

            cartItem.Quantity = dto.Quantity;
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Product quantity updated successfully." });
        }

        // Remove product from cart
        [HttpDelete("remove/{productId}")]
        [Authorize]

        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            int accountId = GetAccountId();

            var cartItem = await _context.Carts
                .FirstOrDefaultAsync(c => c.CartId == accountId && c.ProductId == productId);

            if (cartItem == null)
                return NotFound(new { Message = "Product not found in your cart." });

            _context.Carts.Remove(cartItem);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Product removed from cart successfully." });
        }

        // Clear cart
        [HttpDelete("clear")]
        [Authorize]

        public async Task<IActionResult> ClearCart()
        {
            int accountId = GetAccountId();

            var cartItems = _context.Carts.Where(c => c.CartId == accountId);
            _context.Carts.RemoveRange(cartItems);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "All products removed from cart successfully." });
        }
    }

}
