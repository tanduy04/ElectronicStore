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
    public class BrandsController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly ElectronicStoreContext _context;

        public BrandsController(IWebHostEnvironment env, IConfiguration config, ElectronicStoreContext context)
        {
            _env = env;
            _config = config;
            _context = context;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                var brands = _context.Brands.ToList();
                var baseUrl = $"{Request.Scheme}://{Request.Host}/";

                var result = brands.Select(b => new
                {
                    b.BrandId,
                    b.BrandName,
                    ImageUrl = $"{baseUrl}{_config["ImageSettings:BrandPath"]}{b.BrandImage}",
                    b.IsActive
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            try
            {
                var brand = _context.Brands.Find(id);
                if (brand == null) return NotFound("Brand not found.");

                var baseUrl = $"{Request.Scheme}://{Request.Host}/";

                return Ok(new
                {
                    brand.BrandId,
                    brand.BrandName,
                    ImageUrl = $"{baseUrl}{_config["ImageSettings:BrandPath"]}{brand.BrandImage}",
                    brand.IsActive
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpGet("search")]
        public IActionResult SearchByName(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return BadRequest("Search term is required.");
                }

                var baseUrl = $"{Request.Scheme}://{Request.Host}/";

                var brands = _context.Brands
                    .Where(b => b.BrandName.Contains(name)) // Tìm theo tên
                    .Select(b => new
                    {
                        b.BrandId,
                        b.BrandName,
                        ImageUrl = $"{baseUrl}{_config["ImageSettings:BrandPath"]}{b.BrandImage}",
                        b.IsActive
                    })
                    .ToList();

                if (!brands.Any())
                {
                    return NotFound("Not found");
                }

                return Ok(brands);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Create([FromForm] BrandDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                if (dto.BrandImage == null || !ImageHelper.IsImageFile(dto.BrandImage))
                    return BadRequest("Please upload a valid image file (jpg, jpeg, png, gif).");

                string folderPath = Path.Combine(_env.WebRootPath, _config["ImageSettings:BrandPath"]);
                string fileName = await ImageHelper.SaveImageAsync(dto.BrandImage, folderPath, dto.BrandName);

                var brand = new Brand
                {
                    BrandName = dto.BrandName,
                    BrandImage = fileName,
                    IsActive = dto.IsActive
                };

                _context.Brands.Add(brand);
                await _context.SaveChangesAsync();

                return Ok("Brand created successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Update(int id, [FromForm] BrandDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var brand = _context.Brands.Find(id);
                if (brand == null) return NotFound("Brand not found.");

                brand.BrandName = dto.BrandName;
                brand.IsActive = dto.IsActive;

                if (dto.BrandImage != null)
                {
                    if (!ImageHelper.IsImageFile(dto.BrandImage))
                        return BadRequest("Please upload a valid image file (jpg, jpeg, png, gif).");

                    string folderPath = Path.Combine(_env.WebRootPath, _config["ImageSettings:BrandPath"]);

                    // Xóa ảnh cũ
                    ImageHelper.DeleteFileIfExists(folderPath, brand.BrandImage);

                    // Lưu ảnh mới
                    brand.BrandImage = await ImageHelper.SaveImageAsync(dto.BrandImage, folderPath, dto.BrandName);
                }

                _context.Brands.Update(brand);
                await _context.SaveChangesAsync();

                return Ok("Brand updated successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var brand = _context.Brands.Find(id);
                if (brand == null) return NotFound("Brand not found.");
                bool hasProducts = await _context.Products.AnyAsync(p => p.BrandId == id);
                if (hasProducts)
                {
                    return BadRequest("Cannot delete brand because there are products associated with it.");
                }

                string folderPath = Path.Combine(_env.WebRootPath, _config["ImageSettings:BrandPath"]);
                ImageHelper.DeleteFileIfExists(folderPath, brand.BrandImage);

                _context.Brands.Remove(brand);
                await _context.SaveChangesAsync();

                return Ok("Brand deleted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
