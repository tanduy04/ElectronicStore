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

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
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

    [HttpPost]
    [Authorize(Roles = "Admin,Employee")]

    public async Task<IActionResult> Create([FromForm] CategoryDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        string fileName = null;
        if (dto.CategoryName != null)
        {
            string folderPath = Path.Combine(_env.WebRootPath, _config["ImageSettings:CategoryPath"]);
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            string normalized = ImageHelper.NormalizeFileName(dto.CategoryName);
            string extension = Path.GetExtension(dto.CategoryImage.FileName);
            fileName = $"{normalized}{extension}";

            string fullPath = Path.Combine(folderPath, fileName);
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await dto.CategoryImage.CopyToAsync(stream);
            }
        }

        var category = new Category
        {
            CategoryName = dto.CategoryName,
            CategoryImage = fileName,
            IsActive = dto.IsActive
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return Ok(category);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Employee")]


    public async Task<IActionResult> Update(int id, [FromForm] CategoryDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        var category = _context.Categories.Find(id);
        if (category == null) return NotFound("Category not found.");

        category.CategoryName = dto.CategoryName;
        category.IsActive = dto.IsActive;

        if (dto.CategoryImage != null)
        {
            string folderPath = Path.Combine(_env.WebRootPath, _config["ImageSettings:CategoryPath"]);
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            // Xóa ảnh cũ nếu tồn tại
            if (!string.IsNullOrEmpty(category.CategoryImage))
            {
                string oldPath = Path.Combine(folderPath, category.CategoryImage);
                if (System.IO.File.Exists(oldPath))
                {
                    System.IO.File.Delete(oldPath);
                }
            }

            // Lưu ảnh mới
            string normalized = ImageHelper.NormalizeFileName(dto.CategoryName);
            string extension = Path.GetExtension(dto.CategoryImage.FileName);
            string fileName = $"{normalized}{extension}";

            string fullPath = Path.Combine(folderPath, fileName);
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await dto.CategoryImage.CopyToAsync(stream);
            }

            category.CategoryImage = fileName;
        }

        _context.Categories.Update(category);
        await _context.SaveChangesAsync();

        return Ok("Category updated successfully.");
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Employee")]


    public async Task<IActionResult> Delete(int id)
    {
        var category = _context.Categories.Find(id);
        if (category == null) return NotFound("Category not found.");

        // Xóa file ảnh nếu tồn tại
        if (!string.IsNullOrEmpty(category.CategoryImage))
        {
            string folderPath = Path.Combine(_env.WebRootPath, _config["ImageSettings:CategoryPath"]);
            string filePath = Path.Combine(folderPath, category.CategoryImage);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return Ok("Category deleted successfully.");
    }

}
