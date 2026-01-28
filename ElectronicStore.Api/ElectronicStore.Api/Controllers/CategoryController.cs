using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _categoryService.GetAllCategoriesAsync();

        if (!result.Success)
            return StatusCode(500, result.Message);

        return Ok(result.Data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _categoryService.GetCategoryByIdAsync(id);

        if (!result.Success)
            return NotFound(result.Message);

        return Ok(result.Data);
    }
    [HttpGet("search")]
    public async Task<IActionResult> SearchByName(string name)
    {
        var result = await _categoryService.SearchCategoriesAsync(name);

        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(result.Data);
    }

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

            
            var category = new Category
            {
                CategoryName = dto.CategoryName,
                IsActive = dto.IsActive
            };

            _context.Categories.Add(category);

            await _context.SaveChangesAsync();
            string folderPath = Path.Combine(_env.WebRootPath, _config["ImageSettings:CategoryPath"]);
            string fileName = await ImageHelper.SaveImageAsync(dto.CategoryImage, folderPath, category.CategoryId.ToString());
            category.CategoryImage = fileName;
            _context.Update(category);
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
                category.CategoryImage = await ImageHelper.SaveImageAsync(dto.CategoryImage, folderPath, id.ToString());
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
