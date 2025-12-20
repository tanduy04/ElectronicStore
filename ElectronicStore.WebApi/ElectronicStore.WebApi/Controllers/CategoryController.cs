using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ElectronicStore.WebApi.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;

namespace ElectronicStore.WebApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    
    public class CategoryController : ControllerBase
    {
        ICategoryService _categoryService;
        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }
        [HttpGet]
        public  async Task<IActionResult> Index()
        {
            
            return Ok(await _categoryService.GetCategories());
        }
    }
}
