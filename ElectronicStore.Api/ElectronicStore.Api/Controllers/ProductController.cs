using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.Design.Serialization;

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

        private string GetFolder()
        {
            var relative = _config["ImageSettings:ProductPath"] ?? "Image/Product/";
            return Path.Combine(_env.WebRootPath ?? "wwwroot", relative);
        }

        private string GetBaseUrl()
        {
            return $"{Request.Scheme}://{Request.Host}/";
        }

        // GET: api/products
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll(
    int? categoryId,
    string? sortBy = "CreatedAt",
    string? sortOrder = "desc",
    int pageNumber = 1,
    int pageSize = 1)
        {
            var query = _context.Products.AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            var result = await GetPagedProducts(query, sortBy, sortOrder, pageNumber, pageSize);
            return Ok(result);
        }
        [HttpGet("Search")]
        public async Task<IActionResult> Search(
    string search,
    string? sortBy = "CreatedAt",
    string? sortOrder = "desc",
    int pageNumber = 1,
    int pageSize = 10)
        {
            var query = _context.Products
                .Where(p => p.ProductName.Contains(search));

            var result = await GetPagedProducts(query, sortBy, sortOrder, pageNumber, pageSize);
            return Ok(result);
        }



        // GET: api/products/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == id);

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

        // POST: api/products
        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]

        public async Task<IActionResult> Create([FromForm] ProductDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

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
            await _context.SaveChangesAsync(); // cần ID để đặt tên file

            var folder = GetFolder();
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var normalized = ImageHelper.NormalizeFileName(dto.ProductName);
            var id = product.ProductId;

            // Main image => {normalized}_{id}_1.ext
            if (dto.MainImage != null && dto.MainImage.Length > 0)
            {
                var ext = Path.GetExtension(dto.MainImage.FileName);
                var mainFile = $"{normalized}_{id}_1{ext}";
                var fullMainPath = Path.Combine(folder, mainFile);
                using (var fs = new FileStream(fullMainPath, FileMode.Create))
                {
                    await dto.MainImage.CopyToAsync(fs);
                }

                _context.ProductImages.Add(new ProductImage
                {
                    ProductId = id,
                    UrlProductImage = mainFile,
                    ImageMain = true
                });
            }

            // Sub images => {normalized}_{id}_1.1.ext, {normalized}_{id}_1.2.ext...
            if (dto.SubImages != null && dto.SubImages.Any())
            {
                int idx = 1;
                foreach (var sub in dto.SubImages)
                {
                    if (sub == null || sub.Length == 0) continue;
                    var ext = Path.GetExtension(sub.FileName);
                    var subFile = $"{normalized}_{id}_1.{idx}{ext}";
                    var fullSubPath = Path.Combine(folder, subFile);
                    using (var fs = new FileStream(fullSubPath, FileMode.Create))
                    {
                        await sub.CopyToAsync(fs);
                    }

                    _context.ProductImages.Add(new ProductImage
                    {
                        ProductId = id,
                        UrlProductImage = subFile,
                        ImageMain = false
                    });

                    idx++;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { product.ProductId, message = "Product created successfully" });
        }

        // PUT: api/products/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Employee")]

        public async Task<IActionResult> Update(int id, [FromForm] ProductDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

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

            var folder = GetFolder();
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var normalized = ImageHelper.NormalizeFileName(dto.ProductName);
            var pid = product.ProductId;

            // If new main image provided -> delete old main and save new
            if (dto.MainImage != null && dto.MainImage.Length > 0)
            {
                var oldMain = product.ProductImages.FirstOrDefault(i => i.ImageMain);
                if (oldMain != null)
                {
                    var oldPath = Path.Combine(folder, oldMain.UrlProductImage);
                    ImageHelper.DeleteFileIfExists(oldPath);
                    _context.ProductImages.Remove(oldMain);
                }

                var ext = Path.GetExtension(dto.MainImage.FileName);
                var mainFile = $"{normalized}_{pid}_1{ext}";
                var fullMainPath = Path.Combine(folder, mainFile);
                using (var fs = new FileStream(fullMainPath, FileMode.Create))
                {
                    await dto.MainImage.CopyToAsync(fs);
                }

                _context.ProductImages.Add(new ProductImage
                {
                    ProductId = pid,
                    UrlProductImage = mainFile,
                    ImageMain = true
                });
            }

            // If SubImages provided -> remove old subs and add new subs (re-number from 1)
            if (dto.SubImages != null && dto.SubImages.Any())
            {
                var oldSubs = product.ProductImages.Where(i => !i.ImageMain).ToList();
                foreach (var os in oldSubs)
                {
                    var oldPath = Path.Combine(folder, os.UrlProductImage);
                    ImageHelper.DeleteFileIfExists(oldPath);
                    _context.ProductImages.Remove(os);
                }

                int idx = 1;
                foreach (var sub in dto.SubImages)
                {
                    if (sub == null || sub.Length == 0) continue;
                    var ext = Path.GetExtension(sub.FileName);
                    var subFile = $"{normalized}_{pid}_1.{idx}{ext}";
                    var fullSubPath = Path.Combine(folder, subFile);
                    using (var fs = new FileStream(fullSubPath, FileMode.Create))
                    {
                        await sub.CopyToAsync(fs);
                    }

                    _context.ProductImages.Add(new ProductImage
                    {
                        ProductId = pid,
                        UrlProductImage = subFile,
                        ImageMain = false
                    });

                    idx++;
                }
            }

            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            return Ok("Product updated successfully.");
        }

        // DELETE: api/products/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Employee")]

        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.Include(p => p.ProductImages).FirstOrDefaultAsync(p => p.ProductId == id);
            if (product == null) return NotFound("Product not found.");

            var folder = GetFolder();

            // delete files
            foreach (var img in product.ProductImages)
            {
                var path = Path.Combine(folder, img.UrlProductImage);
                ImageHelper.DeleteFileIfExists(path);
            }

            // remove images and product
            _context.ProductImages.RemoveRange(product.ProductImages);
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok("Product deleted successfully.");
        }
        private async Task<object> GetPagedProducts(IQueryable<Product> query, string? sortBy, string? sortOrder, int pageNumber, int pageSize)
        {
            // Sắp xếp
            query = sortBy?.ToLower() switch
            {
                "name" => (sortOrder == "asc" ? query.OrderBy(p => p.ProductName) : query.OrderByDescending(p => p.ProductName)),
                "price" => (sortOrder == "asc" ? query.OrderBy(p => p.Price) : query.OrderByDescending(p => p.Price)),
                "createdat" => (sortOrder == "asc" ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt)),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            // Phân trang
            var totalItems = await query.CountAsync();
            var products = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Include(p => p.ProductImages)
                .ToListAsync();

            var baseUrl = GetBaseUrl();

            // Trả kết quả
            var result = products.Select(p => new
            {
                p.ProductId,
                p.ProductName,
                p.Description,
                p.Price,
                p.StockQuantity,
                p.IsActive,
                p.ManufactureYear,
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

            return new
            {
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                Data = result
            };
        }

    }
}
