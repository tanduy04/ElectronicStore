using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
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
        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]

        public async Task<IActionResult> Create([FromForm] BrandDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            string fileName = null;
            if (dto.BrandImage != null)
            {
                string folderPath = Path.Combine(_env.WebRootPath, _config["ImageSettings:BrandPath"]);
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                string normalized = ImageHelper.NormalizeFileName(dto.BrandName);
                string extension = Path.GetExtension(dto.BrandImage.FileName);
                fileName = $"{normalized}{extension}";

                string fullPath = Path.Combine(folderPath, fileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await dto.BrandImage.CopyToAsync(stream);
                }
            }

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

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Employee")]


        public async Task<IActionResult> Update(int id, [FromForm] BrandDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var brand = _context.Brands.Find(id);
            if (brand == null) return NotFound("Brand not found.");

            brand.BrandName = dto.BrandName;
            brand.IsActive = dto.IsActive;

            if (dto.BrandImage != null)
            {
                string folderPath = Path.Combine(_env.WebRootPath, _config["ImageSettings:BrandPath"]);
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                // Xóa ảnh cũ
                if (!string.IsNullOrEmpty(brand.BrandImage))
                {
                    string oldPath = Path.Combine(folderPath, brand.BrandImage);
                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }

                // Lưu ảnh mới
                string normalized = ImageHelper.NormalizeFileName(dto.BrandName);
                string extension = Path.GetExtension(dto.BrandImage.FileName);
                string fileName = $"{normalized}{extension}";

                string fullPath = Path.Combine(folderPath, fileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await dto.BrandImage.CopyToAsync(stream);
                }

                brand.BrandImage = fileName;
            }

            _context.Brands.Update(brand);
            await _context.SaveChangesAsync();

            return Ok("Brand updated successfully.");
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Employee")]


        public async Task<IActionResult> Delete(int id)
        {
            var brand = _context.Brands.Find(id);
            if (brand == null) return NotFound("Brand not found.");

            if (!string.IsNullOrEmpty(brand.BrandImage))
            {
                string folderPath = Path.Combine(_env.WebRootPath, _config["ImageSettings:BrandPath"]);
                string filePath = Path.Combine(folderPath, brand.BrandImage);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.Brands.Remove(brand);
            await _context.SaveChangesAsync();

            return Ok("Brand deleted successfully.");
        }
    }

}
