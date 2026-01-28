using ElectronicStore.Api.Dto;

namespace ElectronicStore.Api.Services.Interfaces
{
    public interface ISupplierService
    {
        Task<(bool Success, string Message, object? Data)> GetAllSuppliersAsync();
        Task<(bool Success, string Message, object? Data)> GetSupplierByIdAsync(int id);
        Task<(bool Success, string Message, object? Data)> CreateSupplierAsync(SupplierDto dto);
        Task<(bool Success, string Message)> UpdateSupplierAsync(int id, SupplierDto dto);
        Task<(bool Success, string Message)> DeleteSupplierAsync(int id);
    }
}
