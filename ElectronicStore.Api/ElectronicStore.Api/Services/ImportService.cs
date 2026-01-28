using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Repositories.Interfaces;
using ElectronicStore.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStore.Api.Services
{
    public class ImportService : IImportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ElectronicStoreContext _context;

        public ImportService(IUnitOfWork unitOfWork, ElectronicStoreContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }

        public async Task<(bool Success, string Message, object? Data)> GetAllImportsAsync()
        {
            try
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
                    })
                    .ToListAsync();

                return (true, "Success", imports);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> GetImportByCodeAsync(string importCode)
        {
            try
            {
                var import = await _context.Imports
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
                    })
                    .FirstOrDefaultAsync(i => i.ImportCode == importCode);

                if (import == null)
                    return (false, "Import not found", null);

                return (true, "Success", import);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
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

            return $"{today}{nextNumber}";
        }

        public async Task<(bool Success, string Message, object? Data)> CreateImportAsync(ImportDto dto, int employeeAccountId)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                if (!_context.Suppliers.Any(s => s.SupplierId == dto.SupplierID))
                {
                    return (false, "SupplierID not exists", null);
                }

                if (dto.ImportDetails == null || dto.ImportDetails.Count == 0)
                {
                    return (false, "ImportDetails cannot be empty", null);
                }

                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.AccountId == employeeAccountId);
                if (employee == null)
                    return (false, "Employee not found", null);

                // Validate products
                foreach (var detail in dto.ImportDetails)
                {
                    var product = await _context.Products.FindAsync(detail.ProductID);
                    if (product == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return (false, $"ProductID {detail.ProductID} not found", null);
                    }

                    if (product.SupplierId != dto.SupplierID)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return (false, $"ProductID {detail.ProductID} does not belong to SupplierID {dto.SupplierID}", null);
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

                import.TotalAmount = totalAmount ?? 0;
                await _context.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return (true, "Created Success", null);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return (false, $"An error occurred while processing the request: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> UpdateImportStatusAsync(int importId, string status)
        {
            try
            {
                var import = await _context.Imports.FirstOrDefaultAsync(i => i.ImportId == importId && i.Status == "Pending");
                if (import == null)
                {
                    return (false, "Import not found");
                }

                if (status != "Delivered" && status != "Cancelled")
                {
                    return (false, "Invalid status value");
                }

                if (status == "Delivered")
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

                return (true, "Status updated successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdatePaymentStatusAsync(int importId)
        {
            try
            {
                var import = await _context.Imports.FirstOrDefaultAsync(i => i.ImportId == importId && i.Status == "Delivered" && i.PaymentStatus == "UnPaid");
                if (import == null)
                {
                    return (false, "Import not found");
                }

                import.PaymentStatus = "Paid";
                _context.Imports.Update(import);
                await _context.SaveChangesAsync();

                return (true, "Status updated successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }
    }
}
