using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ElectronicStore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupplierController : ControllerBase
    {
        private readonly ElectronicStoreContext _context;

        public SupplierController(ElectronicStoreContext context)
        {
            _context= context;
        }
        [HttpGet("GetAll")]
        public async Task<ActionResult> GetSuppliers()
        {
            var suppliers= await _context.Suppliers.ToListAsync();
            return Ok(suppliers);
        }

        // 🟢 Lấy 1 Supplier theo ID
        [HttpGet("{id}")]
        public async Task<ActionResult> GetSupplier(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);

            if (supplier == null)
                return NotFound();

            return Ok(supplier);
        }

        [HttpPost]
        public async Task<ActionResult<Supplier>> CreateSupplier([FromForm] SupplierDto Supplier)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var supplier = new Supplier
            {
                SupplierName = Supplier.SupplierName,
                SupplierAddress = Supplier.SupplierAddress,
                SupplierPhone = Supplier.SupplierPhone

            };
            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            return Ok("Created Success");
        }

        // 🟢 Cập nhật Supplier
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSupplier(int id, [FromForm] SupplierDto SupplierDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
                return NotFound();

            supplier.SupplierName = SupplierDto.SupplierName;
            supplier.SupplierAddress = SupplierDto.SupplierAddress;
            supplier.SupplierPhone = SupplierDto.SupplierPhone;
            _context.Update(supplier);
            await _context.SaveChangesAsync();
            return Ok("Updated Success");
        }

        // 🟢 Xóa Supplier
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
                return NotFound();

            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();

            return Ok("Delete susscess");
        }

        
    }
}
