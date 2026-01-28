using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupplierController : ControllerBase
    {
        private readonly ISupplierService _supplierService;

        public SupplierController(ISupplierService supplierService)
        {
            _supplierService = supplierService;
        }

        [HttpGet("GetAll")]
        public async Task<ActionResult> GetSuppliers()
        {
            var result = await _supplierService.GetAllSuppliersAsync();
            
            if (!result.Success)
                return StatusCode(500, result.Message);

            return Ok(result.Data);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetSupplier(int id)
        {
            var result = await _supplierService.GetSupplierByIdAsync(id);
            
            if (!result.Success)
                return NotFound(result.Message);

            return Ok(result.Data);
        }

        [HttpPost]
        public async Task<ActionResult> CreateSupplier([FromForm] SupplierDto supplierDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _supplierService.CreateSupplierAsync(supplierDto);
            
            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Message);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSupplier(int id, [FromForm] SupplierDto supplierDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _supplierService.UpdateSupplierAsync(id, supplierDto);
            
            if (!result.Success)
                return NotFound(result.Message);

            return Ok(result.Message);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            var result = await _supplierService.DeleteSupplierAsync(id);
            
            if (!result.Success)
                return NotFound(result.Message);

            return Ok(result.Message);
        }
    }
}
