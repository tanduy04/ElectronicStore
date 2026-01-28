using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Helper;
using ElectronicStore.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ElectronicStore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        private string GetProductFolder()
        {
            return Path.Combine(_env.WebRootPath, _config["ImageSettings:ProductPath"]);
        }

        private string GetBaseUrl() => _config["AppSettings:BaseUrl"];

        // GET: api/products/GetAll
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll(int? categoryId, int? BrandId, string? sortBy = "CreatedAt", string? sortOrder = "desc", int pageNumber = 1, int pageSize = 12)
        {
            var result = await _productService.GetAllProductsAsync(categoryId, BrandId, sortBy, sortOrder, pageNumber, pageSize);
            if (!result.Success)
                return BadRequest(result.Message);
            return Ok(result.Data);
        }

        // GET: api/products/Search
        [HttpGet("Search")]
        public async Task<IActionResult> Search(string search, string? sortBy = "CreatedAt", string? sortOrder = "desc", int pageNumber = 1, int pageSize = 12)
        {
            var result = await _productService.SearchProductsAsync(search, sortBy, sortOrder, pageNumber, pageSize);
            if (!result.Success)
                return BadRequest(result.Message);
            return Ok(result.Data);
        }

        [HttpGet("FilterBySupllierID{id}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> GetBySupllierId(int id, string? sortBy = "CreatedAt", string? sortOrder = "desc", int pageNumber = 1, int pageSize = 12)
        {
            var result = await _productService.GetProductsBySupplierAsync(id, sortBy, sortOrder, pageNumber, pageSize);
            if (!result.Success)
                return BadRequest(result.Message);
            return Ok(result.Data);
        }

        // GET: api/products/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _productService.GetProductByIdAsync(id);
            if (!result.Success)
                return NotFound(result.Message);
            return Ok(result.Data);
        }


        // POST: api/products
        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Create([FromForm] ProductDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _productService.CreateProductAsync(dto);
            if (!result.Success)
                return BadRequest(result.Message);
            return Ok(result.Data);
        }
        //[HttpPatch("{id}/description")]
        //public async Task<IActionResult> UpdateDescription(int id, [FromBody] UpdateDescriptionDto dto)
        //{
        //    try
        //    {
        //        var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == id);
        //        if (product == null) return NotFound("Product not found.");

        //        // Parse description text to JSON
        //        string? descriptionJson = ParseDescriptionToJson(dto.Description);

        //        product.Description = descriptionJson;
        //        product.UpdatedAt = DateTime.Now;

        //        await _context.SaveChangesAsync();

        //        return Ok(new
        //        {
        //            message = "Description updated successfully",
        //            productId = id,
        //            description = descriptionJson != null
        //                ? JsonSerializer.Deserialize<Dictionary<string, string>>(descriptionJson)
        //                : null
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, "Internal server error: " + ex.Message);
        //    }
        //}
        // PUT: api/products/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Update(int id, [FromForm] ProductDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _productService.UpdateProductAsync(id, dto);
            if (!result.Success)
                return NotFound(result.Message);
            return Ok(result.Message);
        }

        // DELETE: api/products/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _productService.DeleteProductAsync(id);
            if (!result.Success)
                return BadRequest(result.Message);
            return Ok(result.Message);
        }
    }
}
