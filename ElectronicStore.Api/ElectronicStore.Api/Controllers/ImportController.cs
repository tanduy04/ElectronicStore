using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace ElectronicStore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImportController : ControllerBase
    {
        private readonly ElectronicStoreContext _context;

        public ImportController(ElectronicStoreContext context)
        {
            _context=context;
        }
        [HttpGet("getall")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> GetAllImports()
        {

            var imports = await _context.Imports

                .Include(i => i.Employee)
                .Include(i => i.ImportDetails)
                .Include(i => i.Supplier)
                .OrderByDescending(i => i.ImportDate)
                .Select(imports => new
                {
                    imports.ImportId,
                    imports.ImportCode,
                    imports.Supplier.SupplierName,
                    imports.EmployeeId,
                    imports.ImportDate,
                    imports.TotalAmount,
                    imports.Status,
                    imports.PaymentStatus,

                    EmployeeName = imports.Employee.FullName,
                    imports.Note,
                    ImportDetails = imports.ImportDetails.Select(detail => new
                    {
                        detail.ImportDetailId,
                        detail.ProductId,
                        detail.Quantity,
                        detail.CostPrice,
                        detail.TotalPrice,
                    }).ToList()
                }
                )
                .ToListAsync();
            return Ok(imports);
        }
        [HttpGet("{ImportCode}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> GetByImportCode(string ImportCode)
        {
            var imports = await _context.Imports
                .Include(i => i.Employee)
                .Include(i => i.ImportDetails)
                .Include(i => i.Supplier)
                .Select(imports => new
                {
                    imports.ImportId,
                    imports.ImportCode,
                    imports.Supplier.SupplierName,
                    imports.EmployeeId,
                    imports.ImportDate,
                    imports.TotalAmount,
                    imports.Status,
                    imports.PaymentStatus,
                    EmployeeName = imports.Employee.FullName,
                    imports.Note,
                    ImportDetails = imports.ImportDetails.Select(detail => new
                    {
                        detail.ImportDetailId,
                        detail.ProductId,
                        detail.Quantity,
                        detail.CostPrice,
                        detail.TotalPrice,
                    }).ToList()
                }
                )
                .FirstOrDefaultAsync(i => i.ImportCode == ImportCode)

                ;
            return Ok(imports);
        }
        private async Task<string> GenerateImportCodeAsync()
        {
            var today = DateTime.Now.ToString("ddMMyyyy");
            var lastOrder = await _context.Imports
                .Where(o => o.ImportCode.StartsWith(today))
                .OrderByDescending(o => o.ImportCode)
                .FirstOrDefaultAsync();

            int nextNumber = 1001;
            if (lastOrder != null)
            {
                string lastNumberStr = lastOrder.ImportCode.Substring(8);
                if (int.TryParse(lastNumberStr, out int lastNumber))
                    nextNumber = lastNumber + 1;
            }

            var impotcode = $"{today}{nextNumber}";
            return impotcode;
        }
        [HttpPost("create")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Create ([FromBody] ImportDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (!_context.Suppliers.Any(s => s.SupplierId == dto.SupplierID))
            {
                return BadRequest("SupplierID not exists");
            }
            if(dto.ImportDetails == null || dto.ImportDetails.Count == 0)
            {
                return BadRequest("ImportDetails cannot be empty");
            }
            var accountId = User.FindFirst("AccountID")?.Value;
            if (accountId == null) return Unauthorized();
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.AccountId == int.Parse(accountId));
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var detail in dto.ImportDetails)
                {
                    var product = await _context.Products.FindAsync(detail.ProductID);
                    if (product == null)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest($"ProductID {detail.ProductID} not found");
                    }

                    if (product.SupplierId != dto.SupplierID)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest($"ProductID {detail.ProductID} does not belong to SupplierID {dto.SupplierID}");
                    }
                }
                var import = new Import
                {
                    ImportCode = await GenerateImportCodeAsync(),
                    SupplierId = dto.SupplierID,
                    EmployeeId = employee.EmployeeId,
                    ImportDate = DateTime.Now,
                    TotalAmount = 0,
                    Status = "Pending",
                    PaymentStatus = "UnPaid",
                    Note = dto.Note,
                };
                _context.Imports.Add(import);
                await _context.SaveChangesAsync();
                decimal? totalAmount = 0;
                foreach (var detail in dto.ImportDetails)
                {
                    var product = await _context.Products.FindAsync(detail.ProductID);
                    var importDetail = new ImportDetail
                    {
                        ImportId = import.ImportId,
                        ProductId = detail.ProductID,
                        Quantity = detail.Quantity,
                        CostPrice = product.CostPrice,
                        TotalPrice = detail.Quantity * product.CostPrice,
                    };
                    totalAmount += importDetail.TotalPrice;
                    _context.ImportDetails.Add(importDetail);
                }
                var imp = await _context.Imports.FirstOrDefaultAsync(i => i.ImportId == import.ImportId);
                imp.TotalAmount = totalAmount ?? 0;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok("Created Success");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "An error occurred while processing the request.");
            }
            
        }
        [HttpPut("update-status")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> UpdateStatusDelivered([FromForm] int importId, [FromForm] string status)
        {
            var import = await _context.Imports.FirstOrDefaultAsync(i => i.ImportId == importId && i.Status=="Pending");
            if (import == null)
            {
                return NotFound("Import not found");
            }
            
            if (status != "Delivered" && status != "Cancelled")
            {
                return BadRequest("Invalid status value");
            }
            if(status == "Delivered")
            {
                var importDetails = await _context.ImportDetails.Where(d => d.ImportId == importId).ToListAsync();
                foreach (var detail in importDetails)
                {
                    var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == detail.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += detail.Quantity;
                        _context.Products.Update(product);
                    }
                }
                await _context.SaveChangesAsync();
            }
            import.Status = status;
            _context.Imports.Update(import);
            await _context.SaveChangesAsync();
            return Ok("Status updated successfully");
        }
        [HttpPut("update-payment")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> UpdateStatusPayment(int importId)
        {
            var import = await _context.Imports.FirstOrDefaultAsync(i => i.ImportId == importId && i.Status == "Delivered" && i.PaymentStatus =="UnPaid");
            if (import == null)
            {
                return NotFound("Import not found");
            }
            import.PaymentStatus = "Paid";
            _context.Imports.Update(import);
            await _context.SaveChangesAsync();
            
            return Ok("Status updated successfully");
        }

    }
}
