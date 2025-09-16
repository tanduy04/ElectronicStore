using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly ElectronicStoreContext _context;

    public CategoriesController(IWebHostEnvironment env, IConfiguration config, ElectronicStoreContext context)
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
            var categories = _context.Categories.ToList();
            var baseUrl = $"{Request.Scheme}://{Request.Host}/";

            var result = categories.Select(c => new
            {
                c.CategoryId,
                c.CategoryName,
                ImageUrl = $"{baseUrl}{_config["ImageSettings:CategoryPath"]}{c.CategoryImage}",
                c.IsActive
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
            var category = _context.Categories.Find(id);
            if (category == null) return NotFound();

            var baseUrl = $"{Request.Scheme}://{Request.Host}/";

            return Ok(new
            {
                category.CategoryId,
                category.CategoryName,
                ImageUrl = $"{baseUrl}{_config["ImageSettings:CategoryPath"]}{category.CategoryImage}",
                category.IsActive
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Internal server error: " + ex.Message);
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

            var brands = _context.Categories
                .Where(b => b.CategoryName.Contains(name)) // Tìm theo tên
                .Select(b => new
                {
                    b.CategoryId,
                    b.CategoryName,
                    ImageUrl = $"{baseUrl}{_config["ImageSettings:BrandPath"]}{b.CategoryImage}",
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
    public async Task<IActionResult> Create([FromForm] CategoryDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            if (dto.CategoryImage == null || !ImageHelper.IsImageFile(dto.CategoryImage))
                return BadRequest("Please upload a valid image file (jpg, jpeg, png, gif).");

            string folderPath = Path.Combine(_env.WebRootPath, _config["ImageSettings:CategoryPath"]);
            string fileName = await ImageHelper.SaveImageAsync(dto.CategoryImage, folderPath, dto.CategoryName);

            var category = new Category
            {
                CategoryName = dto.CategoryName,
                CategoryImage = fileName,
                IsActive = dto.IsActive
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return Ok("Category created successfully.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Internal server error: " + ex.Message);
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Employee")]
    public async Task<IActionResult> Update(int id, [FromForm] CategoryDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var category = _context.Categories.Find(id);
            if (category == null) return NotFound("Category not found.");

            category.CategoryName = dto.CategoryName;
            category.IsActive = dto.IsActive;

            if (dto.CategoryImage != null)
            {
                if (!ImageHelper.IsImageFile(dto.CategoryImage))
                    return BadRequest("Please upload a valid image file (jpg, jpeg, png, gif).");

                string folderPath = Path.Combine(_env.WebRootPath, _config["ImageSettings:CategoryPath"]);

                // Xóa ảnh cũ nếu có
                ImageHelper.DeleteFileIfExists(folderPath, category.CategoryImage);

                // Lưu ảnh mới
                category.CategoryImage = await ImageHelper.SaveImageAsync(dto.CategoryImage, folderPath, dto.CategoryName);
            }

            _context.Categories.Update(category);
            await _context.SaveChangesAsync();

            return Ok("Category updated successfully.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Internal server error: " + ex.Message);
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Employee")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var category = _context.Categories.Find(id);
            if (category == null) return NotFound("Category not found.");
            bool hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id);
            if (hasProducts)
            {
                return BadRequest("Cannot delete category because there are products associated with it.");
            }
            string folderPath = Path.Combine(_env.WebRootPath, _config["ImageSettings:CategoryPath"]);
            ImageHelper.DeleteFileIfExists(folderPath, category.CategoryImage);

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok("Category deleted successfully.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}
