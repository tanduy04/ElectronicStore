using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Employee")]
    public class VoucherController : ControllerBase
    {
        private readonly IVoucherService _voucherService;

        public VoucherController(IVoucherService voucherService)
        {
            _voucherService = voucherService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _voucherService.GetAllVouchersAsync();
            
            if (!result.Success)
                return StatusCode(500, result.Message);

            return Ok(result.Data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _voucherService.GetVoucherByIdAsync(id);
            
            if (!result.Success)
                return NotFound(result.Message);

            return Ok(result.Data);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] VoucherDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _voucherService.CreateVoucherAsync(model);
            
            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Message);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] VoucherDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _voucherService.UpdateVoucherAsync(id, model);
            
            if (!result.Success)
                return NotFound(result.Message);

            return NoContent();
        }

        // DELETE: api/vouchers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null) return NotFound();
            var isVoucherUsed = await _context.Orders.AnyAsync(o => o.VoucherCode == voucher.VoucherCode);
            if(isVoucherUsed) return BadRequest("Cannot delete voucher that has been used in orders.");
            _context.Vouchers.Remove(voucher);
            await _context.SaveChangesAsync();
            return Ok("Delete susscess");
        }

    }
}
