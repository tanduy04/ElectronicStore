using ElectronicStore.Api.Dto;

namespace ElectronicStore.Api.Services.Interfaces
{
    public interface IVoucherService
    {
        Task<(bool Success, string Message, object? Data)> GetAllVouchersAsync();
        Task<(bool Success, string Message, object? Data)> GetVoucherByIdAsync(int id);
        Task<(bool Success, string Message, object? Data)> GetVoucherByCodeAsync(string code);
        Task<(bool Success, string Message, object? Data)> CreateVoucherAsync(VoucherDto dto);
        Task<(bool Success, string Message)> UpdateVoucherAsync(int id, VoucherDto dto);
        Task<(bool Success, string Message)> DeleteVoucherAsync(int id);
        Task<(bool Success, string Message, object? Data)> ValidateVoucherAsync(string code, decimal orderAmount);
    }
}
