using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ElectronicStoreContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public ProductsController(ElectronicStoreContext context, IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _env = env;
            _config = config;
        }

        private string GetProductFolder()
        {
            return Path.Combine(_env.WebRootPath, _config["ImageSettings:ProductPath"]);
        }

        private string GetBaseUrl() => $"{Request.Scheme}://{Request.Host}/";

        // GET: api/products/GetAll
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll(int? categoryId, string? sortBy = "CreatedAt", string? sortOrder = "desc", int pageNumber = 1, int pageSize = 12)
        {
            try
            {
                var query = _context.Products.AsQueryable();

                if (categoryId.HasValue)
                    query = query.Where(p => p.CategoryId == categoryId.Value);

                var result = await GetPagedProducts(query, sortBy, sortOrder, pageNumber, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        // GET: api/products/Search
        [HttpGet("Search")]
        public async Task<IActionResult> Search(string search, string? sortBy = "CreatedAt", string? sortOrder = "desc", int pageNumber = 1, int pageSize = 12)
        {
            try
            {
                var query = _context.Products.Where(p => p.ProductName.Contains(search));
                var result = await GetPagedProducts(query, sortBy, sortOrder, pageNumber, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }

        }

        // GET: api/products/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var product = await _context.Products.Include(p => p.ProductImages).FirstOrDefaultAsync(p => p.ProductId == id);
                if (product == null) return NotFound("Product not found.");

                var baseUrl = GetBaseUrl();

                return Ok(new
                {
                    product.ProductId,
                    product.ProductName,
                    product.Description,
                    product.Price,
                    product.StockQuantity,
                    product.IsActive,
                    product.ManufactureYear,
                    MainImage = product.ProductImages.FirstOrDefault(i => i.ImageMain) is ProductImage m ? $"{baseUrl}{_config["ImageSettings:ProductPath"]}{m.UrlProductImage}" : null,
                    SubImages = product.ProductImages.Where(i => !i.ImageMain).Select(i => $"{baseUrl}{_config["ImageSettings:ProductPath"]}{i.UrlProductImage}").ToList(),
                    product.CreatedAt,
                    product.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        // POST: api/products
        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Create([FromForm] ProductDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                if (dto.MainImage == null || !ImageHelper.IsImageFile(dto.MainImage))
                    return BadRequest("Please upload a valid main image.");

                if (dto.SubImages != null && dto.SubImages.Any(i => !ImageHelper.IsImageFile(i)))
                    return BadRequest("All sub-images must be valid image files.");

                var product = new Product
                {
                    ProductName = dto.ProductName,
                    Description = dto.Description,
                    ConsumptionCapacity = dto.ConsumptionCapacity,
                    Maintenance = dto.Maintenance,
                    Price = dto.Price,
                    StockQuantity = dto.StockQuantity,
                    CategoryId = dto.CategoryID,
                    BrandId = dto.BrandID,
                    IsActive = dto.IsActive,
                    ManufactureYear = dto.ManufactureYear,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                string folder = GetProductFolder();
                string normalized = ImageHelper.NormalizeFileName(dto.ProductName);

                // Lưu main image
                string mainFile = await ImageHelper.SaveImageAsync(dto.MainImage, folder, $"{normalized}_{product.ProductId}_main");
                _context.ProductImages.Add(new ProductImage { ProductId = product.ProductId, UrlProductImage = mainFile, ImageMain = true });

                // Lưu sub images
                if (dto.SubImages != null && dto.SubImages.Any())
                {
                    int idx = 1;
                    foreach (var sub in dto.SubImages)
                    {
                        string subFile = await ImageHelper.SaveImageAsync(sub, folder, $"{normalized}_{product.ProductId}_sub{idx}");
                        _context.ProductImages.Add(new ProductImage { ProductId = product.ProductId, UrlProductImage = subFile, ImageMain = false });
                        idx++;
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { product.ProductId, message = "Product created successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        // PUT: api/products/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Update(int id, [FromForm] ProductDto dto)
        {
            try
            {
                var product = await _context.Products.Include(p => p.ProductImages).FirstOrDefaultAsync(p => p.ProductId == id);
                if (product == null) return NotFound("Product not found.");

                product.ProductName = dto.ProductName;
                product.Description = dto.Description;
                product.ConsumptionCapacity = dto.ConsumptionCapacity;
                product.Maintenance = dto.Maintenance;
                product.Price = dto.Price;
                product.StockQuantity = dto.StockQuantity;
                product.CategoryId = dto.CategoryID;
                product.BrandId = dto.BrandID;
                product.ManufactureYear = dto.ManufactureYear;
                product.IsActive = dto.IsActive;
                product.UpdatedAt = DateTime.UtcNow;

                string folder = GetProductFolder();
                string normalized = ImageHelper.NormalizeFileName(dto.ProductName);

                // Xóa ảnh cũ nếu có
                foreach (var img in product.ProductImages)
                    ImageHelper.DeleteFileIfExists(folder, img.UrlProductImage);

                _context.ProductImages.RemoveRange(product.ProductImages);

                // Lưu ảnh mới
                string mainFile = await ImageHelper.SaveImageAsync(dto.MainImage, folder, $"{normalized}_{id}_main");
                _context.ProductImages.Add(new ProductImage { ProductId = id, UrlProductImage = mainFile, ImageMain = true });

                if (dto.SubImages != null && dto.SubImages.Any())
                {
                    int idx = 1;
                    foreach (var sub in dto.SubImages)
                    {
                        string subFile = await ImageHelper.SaveImageAsync(sub, folder, $"{normalized}_{id}_sub{idx}");
                        _context.ProductImages.Add(new ProductImage { ProductId = id, UrlProductImage = subFile, ImageMain = false });
                        idx++;
                    }
                }

                await _context.SaveChangesAsync();
                return Ok("Product updated successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        // DELETE: api/products/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var product = await _context.Products.Include(p => p.ProductImages).FirstOrDefaultAsync(p => p.ProductId == id);
                if (product == null) return NotFound("Product not found.");
                var productInCart = await _context.Carts.FirstOrDefaultAsync(p => p.ProductId == id);
                var productInOrder = await _context.OrderDetails.FirstOrDefaultAsync(p => p.ProductId == id);
                if (productInCart != null || productInOrder != null)
                    return BadRequest("This product has been purchased and cannot be deleted.");
                string folder = GetProductFolder();

                foreach (var img in product.ProductImages)
                    ImageHelper.DeleteFileIfExists(folder, img.UrlProductImage);

                _context.ProductImages.RemoveRange(product.ProductImages);
                _context.Products.Remove(product);

                await _context.SaveChangesAsync();
                return Ok("Product deleted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        private async Task<object> GetPagedProducts(IQueryable<Product> query, string? sortBy, string? sortOrder, int pageNumber, int pageSize)
        {
            query = sortBy?.ToLower() switch
            {
                "name" => sortOrder == "asc" ? query.OrderBy(p => p.ProductName) : query.OrderByDescending(p => p.ProductName),
                "price" => sortOrder == "asc" ? query.OrderBy(p => p.Price) : query.OrderByDescending(p => p.Price),
                "createdat" => sortOrder == "asc" ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            var totalItems = await query.CountAsync();
            var products = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).Include(p => p.ProductImages).ToListAsync();
            var baseUrl = GetBaseUrl();

            var result = products.Select(p => new
            {
                p.ProductId,
                p.ProductName,
                p.Description,
                p.Price,
                p.StockQuantity,
                p.IsActive,
                p.ManufactureYear,
                MainImage = p.ProductImages.FirstOrDefault(i => i.ImageMain) is ProductImage m ? $"{baseUrl}{_config["ImageSettings:ProductPath"]}{m.UrlProductImage}" : null,
                SubImages = p.ProductImages.Where(i => !i.ImageMain).Select(i => $"{baseUrl}{_config["ImageSettings:ProductPath"]}{i.UrlProductImage}").ToList(),
                p.CreatedAt,
                p.UpdatedAt
            });

            return new { TotalItems = totalItems, PageNumber = pageNumber, PageSize = pageSize, TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize), Data = result };
        }
    }
}
