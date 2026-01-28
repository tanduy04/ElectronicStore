using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Repositories.Interfaces;
using ElectronicStore.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStore.Api.Services
{
    public class CartService : ICartService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _config;
        private readonly ElectronicStoreContext _context;

        public CartService(
            IUnitOfWork unitOfWork,
            IConfiguration config,
            ElectronicStoreContext context)
        {
            _unitOfWork = unitOfWork;
            _config = config;
            _context = context;
        }

        private string GetBaseUrl() => _config["AppSettings:BaseUrl"];

        public async Task<(bool Success, string Message, object? Data)> GetCartByAccountIdAsync(int accountId)
        {
            try
            {
                var baseUrl = GetBaseUrl();

                var cart = await _context.Carts
                    .Include(c => c.Product)
                        .ThenInclude(p => p.ProductImages)
                    .Where(c => c.CartId == accountId)
                    .ToListAsync();

                if (!cart.Any())
                    return (true, "Your cart is empty.", new List<object>());

                var resultList = new List<object>();
                foreach (var c in cart)
                {
                    var today = DateOnly.FromDateTime(DateTime.Now);
                    var now = TimeOnly.FromDateTime(DateTime.Now);

                    // Check flash sale
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

                    var mainImage = _context.ProductImages
                        .FirstOrDefault(x => x.ProductId == c.ProductId && x.ImageMain == true);

                    resultList.Add(new
                    {
                        c.CartId,
                        c.ProductId,
                        c.Product.ProductName,
                        c.Product.SellPrice,
                        MainImage = mainImage != null 
                            ? $"{baseUrl}{_config["ImageSettings:ProductPath"]}{mainImage.UrlProductImage}" 
                            : null,
                        c.Quantity
                    });
                }

                return (true, "Success", resultList);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> AddToCartAsync(int accountId, AddToCartDto dto)
        {
            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId);
                if (product == null)
                    return (false, "Product not found");

                if (product.StockQuantity < dto.Quantity)
                    return (false, "Not enough stock available");

                var existingCart = await _unitOfWork.Carts.FirstOrDefaultAsync(
                    c => c.CartId == accountId && c.ProductId == dto.ProductId);

                if (existingCart != null)
                {
                    existingCart.Quantity += dto.Quantity;
                    _unitOfWork.Carts.Update(existingCart);
                }
                else
                {
                    var newCart = new Cart
                    {
                        CartId = accountId,
                        ProductId = dto.ProductId,
                        Quantity = dto.Quantity
                    };
                    await _unitOfWork.Carts.AddAsync(newCart);
                }

                await _unitOfWork.SaveChangesAsync();
                return (true, "Product added to cart successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateCartItemAsync(int accountId, int productId, int quantity)
        {
            try
            {
                if (quantity <= 0)
                    return (false, "Quantity must be greater than 0");

                var cartItem = await _unitOfWork.Carts.FirstOrDefaultAsync(
                    c => c.CartId == accountId && c.ProductId == productId);

                if (cartItem == null)
                    return (false, "Cart item not found");

                var product = await _unitOfWork.Products.GetByIdAsync(productId);
                if (product.StockQuantity < quantity)
                    return (false, "Not enough stock available");

                cartItem.Quantity = quantity;
                _unitOfWork.Carts.Update(cartItem);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Cart updated successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> RemoveFromCartAsync(int accountId, int productId)
        {
            try
            {
                var cartItem = await _unitOfWork.Carts.FirstOrDefaultAsync(
                    c => c.CartId == accountId && c.ProductId == productId);

                if (cartItem == null)
                    return (false, "Cart item not found");

                _unitOfWork.Carts.Remove(cartItem);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Item removed from cart successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> ClearCartAsync(int accountId)
        {
            try
            {
                var cartItems = await _unitOfWork.Carts.FindAsync(c => c.CartId == accountId);
                
                if (cartItems.Any())
                {
                    _unitOfWork.Carts.RemoveRange(cartItems);
                    await _unitOfWork.SaveChangesAsync();
                }

                return (true, "Cart cleared successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }
    }
}
