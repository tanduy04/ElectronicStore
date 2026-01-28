using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FlashSaleController : ControllerBase
    {
        private readonly IFlashSaleService _flashSaleService;

        public FlashSaleController(IFlashSaleService flashSaleService)
        {
            _flashSaleService = flashSaleService;
        }
        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _flashSaleService.GetAllFlashSalesAsync();
            if (!result.Success)
                return StatusCode(500, result.Message);
            return Ok(result.Data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetFlashSaleById(int id)
        {
            var result = await _flashSaleService.GetFlashSaleByIdAsync(id);
            if (!result.Success)
                return NotFound(result.Message);
            return Ok(result.Data);
        }

        [HttpGet("get-flashsale-today-and-tomorrow")]
        public async Task<IActionResult> GetFlashSaleTodayAndTomorrow()
        {
            var result = await _flashSaleService.GetFlashSaleTodayAndTomorrowAsync();
            if (!result.Success)
                return NotFound(result.Message);
            return Ok(result.Data);
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpPost]
        public async Task<IActionResult> CreateFlashSale([FromBody] FlashSaleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _flashSaleService.CreateFlashSaleAsync(dto);
            if (!result.Success)
                return BadRequest(result.Message);
            return Ok(result.Message);
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpPost("add-flashsaleItem")]
        public async Task<IActionResult> AddFlashSaleItem([FromBody] FlashSaleItemAddDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _flashSaleService.AddFlashSaleItemAsync(dto);
            if (!result.Success)
                return result.Message.Contains("not found") ? NotFound(result.Message) : BadRequest(result.Message);
            return Ok(result.Message);
        }
        [Authorize(Roles = "Admin,Employee")]
        [HttpPut]
        public async Task<IActionResult> Edit(int id, [FromBody] FlashSaleEditDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _flashSaleService.UpdateFlashSaleAsync(id, dto);
            if (!result.Success)
                return result.Message.Contains("not found") ? NotFound(result.Message) : BadRequest(result.Message);
            return Ok(result.Message);
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpPut("edit-flashsale-item")]
        public async Task<IActionResult> EditFlashSaleItem(int id, [FromBody] FlashSaleItemDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _flashSaleService.UpdateFlashSaleItemAsync(id, dto);
            if (!result.Success)
                return NotFound(result.Message);
            return Ok(result.Message);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _flashSaleService.DeleteFlashSaleAsync(id);
            if (!result.Success)
                return NotFound(result.Message);
            return Ok(result.Message);
        }

        [HttpDelete("delete-flashsale-item")]
        public async Task<IActionResult> DeleteFlashSaleItem(int id)
        {
            var result = await _flashSaleService.DeleteFlashSaleItemAsync(id);
            if (!result.Success)
                return NotFound(result.Message);
            return Ok(result.Message);
        }

        [HttpGet("get-price-flashsale")]
        public async Task<IActionResult> UpdatePrice(int productId, int quantity)
        {
            var result = await _flashSaleService.GetFlashSalePriceAsync(productId, quantity);
            if (!result.Success)
                return result.Message.Contains("not found") ? NotFound(result.Message) : StatusCode(500, result.Message);
            return Ok(result.Data);
        }
    }
}
