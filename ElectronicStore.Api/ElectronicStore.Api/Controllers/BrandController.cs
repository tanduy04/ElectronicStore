using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BrandsController : ControllerBase
    {
        private readonly IBrandService _brandService;

        public BrandsController(IBrandService brandService)
        {
            _brandService = brandService;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _brandService.GetAllBrandsAsync();

            if (!result.Success)
                return StatusCode(500, result.Message);

            return Ok(result.Data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _brandService.GetBrandByIdAsync(id);

            if (!result.Success)
                return NotFound(result.Message);

            return Ok(result.Data);
        }
        [HttpGet("get-by-categoryId/{id}")]
        public IActionResult GetByCategoriesID(int id=0)
        {
            try
            {
                var baseUrl = _config["AppSettings:BaseUrl"];
                object brand = null;
                if (id != 0)
                {
                    brand = _context.Products
                    .Where(p => p.CategoryId == id)
                    .Select(b => new
                    {
                        b.Brand.BrandId,
                        b.Brand.BrandName,
                        ImageUrl = $"{baseUrl}{_config["ImageSettings:BrandPath"]}{b.Brand.BrandImage}",
                        b.Brand.IsActive
                    })
                    .Distinct()
                    .ToList();
                }
                else
                {
                   brand = _context.Brands
                    .Select(b => new
                    {
                        b.BrandId,
                        b.BrandName,
                        ImageUrl = $"{baseUrl}{_config["ImageSettings:BrandPath"]}{b.BrandImage}",
                        b.IsActive
                    })
                    .ToList();
                }


                if (brand == null) return NotFound("Brand not found.");

                return Ok(brand);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpGet("search")]
        public async Task<IActionResult> SearchByName(string name)
        {
            var result = await _brandService.SearchBrandsAsync(name);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Data);
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

                

                var brand = new Brand
                {
                    BrandName = dto.BrandName,
                    IsActive = dto.IsActive
                };

                _context.Brands.Add(brand);
                await _context.SaveChangesAsync();
                string folderPath = Path.Combine(_env.WebRootPath, _config["ImageSettings:BrandPath"]);
                string fileName = await ImageHelper.SaveImageAsync(dto.BrandImage, folderPath, brand.BrandId.ToString());
                brand.BrandImage = fileName;
                _context.Brands.Update(brand);
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
                    brand.BrandImage = await ImageHelper.SaveImageAsync(dto.BrandImage, folderPath,id.ToString());
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
