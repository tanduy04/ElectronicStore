using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Employee")]
    public class VoucherController : ControllerBase
    {
        private readonly ElectronicStoreContext _context;

        public VoucherController(ElectronicStoreContext context)
        {
            _context = context;
        }

        // GET: api/vouchers
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var vouchers = await _context.Vouchers.ToListAsync();
            return Ok(vouchers);
        }

        // GET: api/vouchers/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null) return NotFound();
            return Ok(voucher);
        }

        // POST: api/vouchers
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] VoucherDto model)
        {
            if(!ModelState.IsValid) return BadRequest(ModelState);
            Voucher voucher = new Voucher
            {
                VoucherCode = model.VoucherCode,
                VoucherName = model.VoucherName,
                DiscountType = model.DiscountType,
                DiscountValue = model.DiscountValue,
                Quantity = model.Quantity,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                IsActive = model.IsActive
            };
            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();
            return Ok("Created successfully");
        }

        // PUT: api/vouchers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] VoucherDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var voucherExists = await _context.Vouchers.FindAsync(id);
            if (voucherExists == null) return NotFound();
            voucherExists.VoucherCode = model.VoucherCode;
            voucherExists.VoucherName = model.VoucherName;
            voucherExists.DiscountType = model.DiscountType;
            voucherExists.DiscountValue = model.DiscountValue;
            voucherExists.Quantity = model.Quantity;
            voucherExists.StartDate = model.StartDate;
            voucherExists.EndDate = model.EndDate;
            voucherExists.IsActive = model.IsActive;
             _context.Vouchers.Update(voucherExists);
            await _context.SaveChangesAsync();
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
