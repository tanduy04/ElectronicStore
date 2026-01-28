using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Repositories.Interfaces;
using ElectronicStore.Api.Services.Interfaces;

namespace ElectronicStore.Api.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SupplierService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<(bool Success, string Message, object? Data)> GetAllSuppliersAsync()
        {
            try
            {
                var suppliers = await _unitOfWork.Suppliers.GetAllAsync();
                return (true, "Success", suppliers);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> GetSupplierByIdAsync(int id)
        {
            try
            {
                var supplier = await _unitOfWork.Suppliers.GetByIdAsync(id);
                if (supplier == null)
                    return (false, "Supplier not found", null);

                return (true, "Success", supplier);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> CreateSupplierAsync(SupplierDto dto)
        {
            try
            {
                var supplier = new Supplier
                {
                    SupplierName = dto.SupplierName,
                    SupplierAddress = dto.SupplierAddress,
                    SupplierPhone = dto.SupplierPhone
                };

                await _unitOfWork.Suppliers.AddAsync(supplier);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Supplier created successfully", supplier);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> UpdateSupplierAsync(int id, SupplierDto dto)
        {
            try
            {
                var supplier = await _unitOfWork.Suppliers.GetByIdAsync(id);
                if (supplier == null)
                    return (false, "Supplier not found");

                supplier.SupplierName = dto.SupplierName;
                supplier.SupplierAddress = dto.SupplierAddress;
                supplier.SupplierPhone = dto.SupplierPhone;

                _unitOfWork.Suppliers.Update(supplier);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Supplier updated successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteSupplierAsync(int id)
        {
            try
            {
                var supplier = await _unitOfWork.Suppliers.GetByIdAsync(id);
                if (supplier == null)
                    return (false, "Supplier not found");

                _unitOfWork.Suppliers.Remove(supplier);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Supplier deleted successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }
    }
}
