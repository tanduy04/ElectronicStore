using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImportController : ControllerBase
    {
        private readonly IImportService _importService;

        public ImportController(IImportService importService)
        {
            _importService = importService;
        }
        [HttpGet("getall")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> GetAllImports()
        {
            var result = await _importService.GetAllImportsAsync();
            if (!result.Success)
                return StatusCode(500, result.Message);
            return Ok(result.Data);
        }

        [HttpGet("{ImportCode}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> GetByImportCode(string ImportCode)
        {
            var result = await _importService.GetImportByCodeAsync(ImportCode);
            if (!result.Success)
                return NotFound(result.Message);
            return Ok(result.Data);
        }
        [HttpPost("create")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Create([FromBody] ImportDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var accountId = User.FindFirst("AccountID")?.Value;
            if (accountId == null) return Unauthorized();

            var result = await _importService.CreateImportAsync(dto, int.Parse(accountId));
            if (!result.Success)
                return BadRequest(result.Message);
            return Ok(result.Message);
        }

        [HttpPut("update-status")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> UpdateStatusDelivered([FromForm] int importId, [FromForm] string status)
        {
            var result = await _importService.UpdateImportStatusAsync(importId, status);
            if (!result.Success)
                return result.Message.Contains("not found") ? NotFound(result.Message) : BadRequest(result.Message);
            return Ok(result.Message);
        }

        [HttpPut("update-payment")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> UpdateStatusPayment(int importId)
        {
            var result = await _importService.UpdatePaymentStatusAsync(importId);
            if (!result.Success)
                return NotFound(result.Message);
            return Ok(result.Message);
        }
    }
}
