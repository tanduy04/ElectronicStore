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

    [HttpPost]
    [Authorize(Roles = "Admin,Employee")]
    public async Task<IActionResult> Create([FromForm] CategoryDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _categoryService.CreateCategoryAsync(dto);

        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(result.Message);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Employee")]
    public async Task<IActionResult> Update(int id, [FromForm] CategoryDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _categoryService.UpdateCategoryAsync(id, dto);

        if (!result.Success)
            return result.Message.Contains("not found") ? NotFound(result.Message) : BadRequest(result.Message);

        return Ok(result.Message);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Employee")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _categoryService.DeleteCategoryAsync(id);

        if (!result.Success)
            return result.Message.Contains("not found") ? NotFound(result.Message) : BadRequest(result.Message);

        return Ok(result.Message);
    }
}