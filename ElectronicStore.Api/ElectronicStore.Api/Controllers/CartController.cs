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
                c.Product.DiscountPrice,
                c.Quantity
            });

            return Ok(result);
        }

        // Add product to cart
        [HttpPost("add")]

        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            try
            {
                var productExists = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == dto.ProductId);
                if (productExists == null)
                {
                    return BadRequest("Product not found");
                }
                int accountId = GetAccountId();

                int quantity = dto.Quantity;

                var cartItem = await _context.Carts
                    .FirstOrDefaultAsync(c => c.CartId == accountId && c.ProductId == dto.ProductId);

                if (cartItem == null)
                {
                    if (productExists.StockQuantity >= quantity)
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
                        return BadRequest("insufficient inventory");
                    }
                }
                else
                {
                    if (productExists.StockQuantity >= (cartItem.Quantity + quantity))
                        cartItem.Quantity += quantity;
                    else
                    {
                        return BadRequest("insufficient inventory");
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { Message = "Product added to cart successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }


        // Update product quantity
        [HttpPut("update")]

        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateCartDto dto)
        {
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        // Remove product from cart
        [HttpDelete("remove/{productId}")]

        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        // Clear cart
        [HttpDelete("clear")]
        [Authorize]

        public async Task<IActionResult> ClearCart()
        {
            try
            {
                int accountId = GetAccountId();

                var cartItems = _context.Carts.Where(c => c.CartId == accountId);
                _context.Carts.RemoveRange(cartItems);
                await _context.SaveChangesAsync();
                return Ok(new { Message = "All products removed from cart successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }
       
    }

}
