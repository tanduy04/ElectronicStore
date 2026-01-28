using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Repositories.Interfaces;
using ElectronicStore.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStore.Api.Services
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public ProductService(
            IUnitOfWork unitOfWork,
            IConfiguration config,
            IWebHostEnvironment env)
        {
            _unitOfWork = unitOfWork;
            _config = config;
            _env = env;
        }

        private string GetBaseUrl() => _config["AppSettings:BaseUrl"];

        public async Task<(bool Success, string Message, object? Data)> GetAllProductsAsync(
            int? categoryId,
            int? brandId,
            string? sortBy,
            string? sortOrder,
            int pageNumber,
            int pageSize)
        {
            try
            {
                var query = _unitOfWork.Products.FindAsync(p => true);
                var products = await query;

                // Filter
                if (categoryId.HasValue)
                    products = products.Where(p => p.CategoryId == categoryId.Value);

                if (brandId.HasValue)
                    products = products.Where(p => p.BrandId == brandId.Value);

                // Sort
                products = ApplySorting(products, sortBy, sortOrder);

                // Pagination
                var totalItems = products.Count();
                var items = products
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => MapToDto(p))
                    .ToList();

                var result = new
                {
                    items,
                    totalItems,
                    pageNumber,
                    pageSize,
                    totalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
                };

                return (true, "Success", result);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> SearchProductsAsync(
            string search,
            string? sortBy,
            string? sortOrder,
            int pageNumber,
            int pageSize)
        {
            try
            {
                var products = await _unitOfWork.Products.SearchProductsAsync(search);

                // Sort
                var sortedProducts = ApplySorting(products, sortBy, sortOrder);

                // Pagination
                var totalItems = sortedProducts.Count();
                var items = sortedProducts
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => MapToDto(p))
                    .ToList();

                var result = new
                {
                    items,
                    totalItems,
                    pageNumber,
                    pageSize,
                    totalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
                };

                return (true, "Success", result);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> GetProductByIdAsync(int id)
        {
            try
            {
                var product = await _unitOfWork.Products.GetProductWithDetailsAsync(id);
                if (product == null)
                    return (false, "Product not found", null);

                var productDto = MapToDetailDto(product);
                return (true, "Success", productDto);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> CreateProductAsync(ProductDto dto)
        {
            try
            {
                var product = new Product
                {
                    ProductName = dto.ProductName,
                    Description = dto.Description,
                    Price = dto.Price,
                    StockQuantity = dto.StockQuantity,
                    CategoryId = dto.CategoryId,
                    BrandId = dto.BrandId,
                    SupplierId = dto.SupplierId,
                    CreatedAt = DateTime.Now
                };

                await _unitOfWork.Products.AddAsync(product);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Product created successfully", product);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> UpdateProductAsync(int id, ProductDto dto)
        {
            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(id);
                if (product == null)
                    return (false, "Product not found");

                product.ProductName = dto.ProductName;
                product.Description = dto.Description;
                product.Price = dto.Price;
                product.StockQuantity = dto.StockQuantity;
                product.CategoryId = dto.CategoryId;
                product.BrandId = dto.BrandId;
                product.SupplierId = dto.SupplierId;

                _unitOfWork.Products.Update(product);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Product updated successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteProductAsync(int id)
        {
            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(id);
                if (product == null)
                    return (false, "Product not found");

                _unitOfWork.Products.Remove(product);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Product deleted successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message, object? Data)> GetProductsBySupplierAsync(
            int supplierId,
            string? sortBy,
            string? sortOrder,
            int pageNumber,
            int pageSize)
        {
            try
            {
                var products = await _unitOfWork.Products.GetProductsBySupplierAsync(supplierId);

                // Sort
                var sortedProducts = ApplySorting(products, sortBy, sortOrder);

                // Pagination
                var totalItems = sortedProducts.Count();
                var items = sortedProducts
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => MapToDto(p))
                    .ToList();

                var result = new
                {
                    items,
                    totalItems,
                    pageNumber,
                    pageSize,
                    totalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
                };

                return (true, "Success", result);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        private IEnumerable<Product> ApplySorting(IEnumerable<Product> products, string? sortBy, string? sortOrder)
        {
            var isDescending = sortOrder?.ToLower() == "desc";

            return sortBy?.ToLower() switch
            {
                "price" => isDescending ? products.OrderByDescending(p => p.Price) : products.OrderBy(p => p.Price),
                "name" => isDescending ? products.OrderByDescending(p => p.ProductName) : products.OrderBy(p => p.ProductName),
                "createdat" => isDescending ? products.OrderByDescending(p => p.CreatedAt) : products.OrderBy(p => p.CreatedAt),
                _ => products.OrderByDescending(p => p.CreatedAt)
            };
        }

        private object MapToDto(Product product)
        {
            var baseUrl = GetBaseUrl();
            return new
            {
                product.ProductId,
                product.ProductName,
                product.Price,
                product.StockQuantity,
                product.CategoryId,
                product.BrandId,
                ImageUrl = product.ProductImages?.FirstOrDefault()?.ImageUrl != null
                    ? $"{baseUrl}{_config["ImageSettings:ProductPath"]}{product.ProductImages.FirstOrDefault()?.ImageUrl}"
                    : null
            };
        }

        private object MapToDetailDto(Product product)
        {
            var baseUrl = GetBaseUrl();
            return new
            {
                product.ProductId,
                product.ProductName,
                product.Description,
                product.Price,
                product.StockQuantity,
                product.CategoryId,
                CategoryName = product.Category?.CategoryName,
                product.BrandId,
                BrandName = product.Brand?.BrandName,
                Images = product.ProductImages?.Select(img => $"{baseUrl}{_config["ImageSettings:ProductPath"]}{img.ImageUrl}").ToList()
            };
        }
    }
}
