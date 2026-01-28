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
        public async Task<IActionResult> GetByCategoriesID(int id = 0)
        {
            var result = await _brandService.GetBrandsByCategoryIdAsync(id);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Data);
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
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _brandService.CreateBrandAsync(dto);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Message);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Update(int id, [FromForm] BrandDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _brandService.UpdateBrandAsync(id, dto);

            if (!result.Success)
                return result.Message.Contains("not found") ? NotFound(result.Message) : BadRequest(result.Message);

            return Ok(result.Message);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _brandService.DeleteBrandAsync(id);

            if (!result.Success)
                return result.Message.Contains("not found") ? NotFound(result.Message) : BadRequest(result.Message);

            return Ok(result.Message);
        }
    }
}
