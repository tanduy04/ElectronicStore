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
        private readonly IConfiguration _config;

        public CartController(ElectronicStoreContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }
        private string GetBaseUrl() => $"{Request.Scheme}://{Request.Host}/";
        // Get AccountId from token
        private int GetAccountId()
        {
            return int.Parse(User.FindFirst("AccountID").Value);
        }
        
        // Get user's cart
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var baseUrl = GetBaseUrl();
            int accountId = GetAccountId();

            var cart = await _context.Carts
                .Include(c => c.Product).ThenInclude(c => c.ProductImages)
                .Where(c => c.CartId == accountId)
                .ToListAsync();

            if (!cart.Any())
                return Ok(new { Message = "Your cart is empty." });
            var resultList = new List<object>();
            foreach (var c in cart)
            {
                var today = DateOnly.FromDateTime(System.DateTime.Now);
                var now = TimeOnly.FromDateTime(System.DateTime.Now);
                var flashSaleItem = await _context.FlashSaleItems
                    .Include(fsi => fsi.FlashSale)
                    .Where(fsi => fsi.ProductId == c.ProductId &&
                                  fsi.FlashSale.DateSale == today &&
                                  fsi.FlashSale.StartTime <= now &&
                                  fsi.FlashSale.EndTime >= now &&
                                  fsi.Quantity >= c.Quantity)
                    .FirstOrDefaultAsync();
                if (flashSaleItem != null)
                    c.Product.SellPrice = flashSaleItem.SellPrice;
                resultList.Add(new
                {
                    c.CartId,
                    c.ProductId,
                    c.Product.ProductName,
                    c.Product.SellPrice,
                    MainImage = _context.ProductImages.FirstOrDefault(x => x.ProductId == c.ProductId && x.ImageMain == true) is ProductImage m ? $"{baseUrl}{_config["ImageSettings:ProductPath"]}{m.UrlProductImage}" : null,
                    c.Quantity
                });
            }


            return Ok(resultList);
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
                var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == dto.ProductId);
                if (product.StockQuantity < dto.Quantity)
                    return BadRequest(new { Message = "Insufficient inventory." });
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
