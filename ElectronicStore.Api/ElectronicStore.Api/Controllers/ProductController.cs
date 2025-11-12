using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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
        public async Task<IActionResult> GetAll(int? categoryId, int? BrandId, string? sortBy = "CreatedAt", string? sortOrder = "desc", int pageNumber = 1, int pageSize = 12)
        {
            try
            {
                var query = _context.Products.AsQueryable();

                if (categoryId.HasValue)
                    query = query.Where(p => p.CategoryId == categoryId.Value);
                if (BrandId.HasValue)
                    query = query.Where(p => p.BrandId == BrandId.Value);

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

        [HttpGet("FilterBySupllierID{id}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> GetBySupllierId(int id, string? sortBy = "CreatedAt", string? sortOrder = "desc", int pageNumber = 1, int pageSize = 12)
        {
            try
            {
                var query = _context.Products.Where(p => p.SupplierId == id);
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
                var product = await _context.Products.Include(p => p.ProductImages).Include(p => p.Brand).Include(p => p.Category).FirstOrDefaultAsync(p => p.ProductId == id);
                if (product == null) return NotFound("Product not found.");

                var baseUrl = GetBaseUrl();
                var today = DateOnly.FromDateTime(System.DateTime.Now);
                var now = TimeOnly.FromDateTime(System.DateTime.Now);
                var flashSaleItem = await _context.FlashSaleItems
                    .Include(fsi => fsi.FlashSale)
                    .Where(fsi => fsi.ProductId == product.ProductId &&
                                  fsi.FlashSale.DateSale == today &&
                                  fsi.FlashSale.StartTime <= now &&
                                  fsi.FlashSale.EndTime >= now &&
                                  fsi.Quantity > 0)
                    .FirstOrDefaultAsync();
                if (flashSaleItem != null)
                    product.SellPrice = flashSaleItem.SellPrice;

                // Parse description from JSON
                Dictionary<string, string>? descriptionObj = null;
                if (!string.IsNullOrEmpty(product.Description))
                {
                    try
                    {
                        descriptionObj = JsonSerializer.Deserialize<Dictionary<string, string>>(product.Description);
                    }
                    catch
                    {
                        // If not JSON, keep as is
                        descriptionObj = new Dictionary<string, string> { { "Description", product.Description } };
                    }
                }

                return Ok(new
                {
                    product.ProductId,
                    product.ProductName,
                    Description = descriptionObj,
                    product.CostPrice,
                    product.SellPrice,
                    product.Maintenance,
                    product.OriginalPrice,
                    product.StockQuantity,
                    product.Brand.BrandId,
                    product.Brand.BrandName,
                    product.Category.CategoryId,
                    product.Category.CategoryName,
                    product.IsActive,
                    product.ManufactureYear,
                    SoldQuantity = _context.OrderDetails.Where(od => od.ProductId == product.ProductId).Sum(od => (int?)od.Quantity) ?? 0,
                    ProductReview = await GetReviewByProductId(id),
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

                if (dto.SubImages == null || dto.SubImages.Any(i => !ImageHelper.IsImageFile(i)))
                    return BadRequest("All sub-images must be valid image files.");
                if (dto.OriginalPrice == null || dto.OriginalPrice <= 0)
                    dto.OriginalPrice = dto.SellPrice;

                // Parse description text to JSON
                string? descriptionJson = ParseDescriptionToJson(dto.Description);

                var product = new Product
                {
                    ProductName = dto.ProductName,
                    Description = descriptionJson,
                    Maintenance = dto.Maintenance,
                    CostPrice = dto.CostPrice,
                    OriginalPrice = dto.OriginalPrice,
                    SellPrice = dto.SellPrice,
                    StockQuantity = dto.StockQuantity,
                    CategoryId = dto.CategoryID,
                    BrandId = dto.BrandID,
                    SupplierId = dto.SupplierID,
                    IsActive = dto.IsActive,
                    ManufactureYear = dto.ManufactureYear,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                string folder = GetProductFolder();

                // Lưu main image
                string mainFile = await ImageHelper.SaveImageAsync(dto.MainImage, folder, $"{product.ProductId}_main");
                _context.ProductImages.Add(new ProductImage { ProductId = product.ProductId, UrlProductImage = mainFile, ImageMain = true });

                // Lưu sub images
                if (dto.SubImages != null && dto.SubImages.Any())
                {
                    int idx = 1;
                    foreach (var sub in dto.SubImages)
                    {
                        string subFile = await ImageHelper.SaveImageAsync(sub, folder, $"{product.ProductId}_sub{idx}");
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
        //[HttpPatch("{id}/description")]
        //public async Task<IActionResult> UpdateDescription(int id, [FromBody] UpdateDescriptionDto dto)
        //{
        //    try
        //    {
        //        var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == id);
        //        if (product == null) return NotFound("Product not found.");

        //        // Parse description text to JSON
        //        string? descriptionJson = ParseDescriptionToJson(dto.Description);

        //        product.Description = descriptionJson;
        //        product.UpdatedAt = DateTime.UtcNow;

        //        await _context.SaveChangesAsync();

        //        return Ok(new
        //        {
        //            message = "Description updated successfully",
        //            productId = id,
        //            description = descriptionJson != null
        //                ? JsonSerializer.Deserialize<Dictionary<string, string>>(descriptionJson)
        //                : null
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, "Internal server error: " + ex.Message);
        //    }
        //}
        // PUT: api/products/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Update(int id, [FromForm] ProductDto dto)
        {
            try
            {
                var product = await _context.Products.Include(p => p.ProductImages).FirstOrDefaultAsync(p => p.ProductId == id);
                if (product == null) return NotFound("Product not found.");
                if (dto.OriginalPrice == null || dto.OriginalPrice <= 0)
                    dto.OriginalPrice = dto.SellPrice;

                // Parse description text to JSON
                string? descriptionJson = ParseDescriptionToJson(dto.Description);

                product.ProductName = dto.ProductName;
                product.Description = descriptionJson;
                product.Maintenance = dto.Maintenance;
                product.CostPrice = dto.CostPrice;
                product.OriginalPrice = dto.OriginalPrice;
                product.SellPrice = dto.SellPrice;
                product.StockQuantity = dto.StockQuantity;
                product.CategoryId = dto.CategoryID;
                product.BrandId = dto.BrandID;
                product.SupplierId = dto.SupplierID;
                product.ManufactureYear = dto.ManufactureYear;
                product.IsActive = dto.IsActive;
                product.UpdatedAt = DateTime.UtcNow;

                string folder = GetProductFolder();

                if (dto.MainImage != null)
                {
                    if (ImageHelper.IsImageFile(dto.MainImage))
                    {
                        var mainImage = _context.ProductImages.FirstOrDefault(p => p.ProductId == product.ProductId && p.ImageMain == true);
                        ImageHelper.DeleteFileIfExists(folder, mainImage.UrlProductImage);
                        _context.ProductImages.RemoveRange(mainImage);
                        string mainFile = await ImageHelper.SaveImageAsync(dto.MainImage, folder, $"{id}_main");
                        _context.ProductImages.Add(new ProductImage { ProductId = id, UrlProductImage = mainFile, ImageMain = true });
                    }

                }

                if (dto.SubImages != null)
                {
                    if (dto.SubImages.Any(i => ImageHelper.IsImageFile(i)))
                    {
                        var subImage = _context.ProductImages.Where(p => p.ProductId == product.ProductId && p.ImageMain == false);
                        foreach (var img in subImage)
                            ImageHelper.DeleteFileIfExists(folder, img.UrlProductImage);

                        _context.ProductImages.RemoveRange(subImage);
                        if (dto.SubImages != null && dto.SubImages.Any())
                        {
                            int idx = 1;
                            foreach (var sub in dto.SubImages)
                            {
                                string subFile = await ImageHelper.SaveImageAsync(sub, folder, $"{id}_sub{idx}");
                                _context.ProductImages.Add(new ProductImage { ProductId = id, UrlProductImage = subFile, ImageMain = false });
                                idx++;
                            }
                        }
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
        private async Task<object> GetReviewByProductId(int id)
        {
            var reviews = await _context.ProductReviews
                    .Where(r => r.ProductId == id && r.ParentId == null && r.IsActive == true)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new ProductReviewDto
                    {
                        ReviewId = r.ReviewId,
                        ProductId = r.ProductId,
                        FullName = r.FullName,
                        Phone = r.Phone,
                        Rating = r.Rating,
                        ParentId = r.ParentId,
                        Content = r.Content,
                    })
                    .ToListAsync();
            if (reviews == null || reviews.Count == 0)
                return null;
            foreach (var review in reviews)
            {
                var childReview = await _context.ProductReviews
                     .Where(cr => cr.ParentId == review.ReviewId)
                     .OrderByDescending(r => r.CreatedAt)
                     .Select(r => new ViewReplyReview
                     {
                         ParentID = r.ParentId.Value,
                         ReviewID = r.ReviewId,
                         Name = r.FullName,
                         Content = r.Content,
                     })
           .FirstOrDefaultAsync();
                if (childReview != null)
                    review.ReplyReview = childReview;
            }
            return reviews;
        }
        private async Task<object> GetPagedProducts(IQueryable<Product> query, string? sortBy, string? sortOrder, int pageNumber, int pageSize)
        {
            query = sortBy?.ToLower() switch
            {
                "name" => sortOrder == "asc" ? query.OrderBy(p => p.ProductName) : query.OrderByDescending(p => p.ProductName),
                "price" => sortOrder == "asc" ? query.OrderBy(p => p.OriginalPrice) : query.OrderByDescending(p => p.OriginalPrice),
                "createdat" => sortOrder == "asc" ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            var totalItems = await query.CountAsync();
            var products = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Include(p => p.ProductImages)
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Supplier)
                .ToListAsync();

            var baseUrl = GetBaseUrl();

            var resultList = new List<object>();
            foreach (var p in products)
            {
                var reviews = await GetReviewByProductId(p.ProductId);
                var today = DateOnly.FromDateTime(System.DateTime.Now);
                var now = TimeOnly.FromDateTime(System.DateTime.Now);
                var flashSaleItem = await _context.FlashSaleItems
                    .Include(fsi => fsi.FlashSale)
                    .Where(fsi => fsi.ProductId == p.ProductId &&
                                  fsi.FlashSale.DateSale == today &&
                                  fsi.FlashSale.StartTime <= now &&
                                  fsi.FlashSale.EndTime >= now &&
                                  fsi.Quantity > 0)
                    .FirstOrDefaultAsync();
                if (flashSaleItem != null)
                    p.SellPrice = flashSaleItem.SellPrice;

                // Parse description from JSON
                Dictionary<string, string>? descriptionObj = null;
                if (!string.IsNullOrEmpty(p.Description))
                {
                    try
                    {
                        descriptionObj = JsonSerializer.Deserialize<Dictionary<string, string>>(p.Description);
                    }
                    catch
                    {
                        // If not JSON, keep as is
                        descriptionObj = new Dictionary<string, string> { { "Description", p.Description } };
                    }
                }

                resultList.Add(new
                {
                    p.ProductId,
                    p.ProductName,
                    Description = descriptionObj,
                    p.CostPrice,
                    p.SellPrice,
                    p.Maintenance,
                    p.OriginalPrice,
                    p.StockQuantity,
                    p.IsActive,
                    p.Brand.BrandId,
                    p.Supplier.SupplierId,
                    p.Brand.BrandName,
                    p.Category.CategoryId,
                    p.Category.CategoryName,
                    p.ManufactureYear,
                    SoldQuantity = _context.OrderDetails
                        .Where(od => od.ProductId == p.ProductId)
                        .Sum(od => (int?)od.Quantity) ?? 0,
                    ProductReview = reviews,
                    MainImage = p.ProductImages.FirstOrDefault(i => i.ImageMain) is ProductImage m
                        ? $"{baseUrl}{_config["ImageSettings:ProductPath"]}{m.UrlProductImage}"
                        : null,
                    SubImages = p.ProductImages
                        .Where(i => !i.ImageMain)
                        .Select(i => $"{baseUrl}{_config["ImageSettings:ProductPath"]}{i.UrlProductImage}")
                        .ToList(),
                    p.CreatedAt,
                    p.UpdatedAt
                });
            }

            return new
            {
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                Data = resultList
            };
        }

        // Helper method to parse plain text description to JSON
        // Input format:
        // Công suất: 1000W
        // Năm sản xuất: 2024
        // Bảo hành: 12 tháng
        private string? ParseDescriptionToJson(string? descriptionText)
        {
            if (string.IsNullOrWhiteSpace(descriptionText))
                return null;

            var dict = new Dictionary<string, string>();
            var lines = descriptionText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var parts = line.Split(new[] { ':' }, 2); // Chỉ split tại dấu ':' đầu tiên
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();
                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                    {
                        dict[key] = value;
                    }
                }
            }

            return dict.Any() ? JsonSerializer.Serialize(dict) : null;
        }
    }
}
