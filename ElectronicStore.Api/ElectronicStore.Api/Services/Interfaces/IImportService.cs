using ElectronicStore.Api.Dto;

namespace ElectronicStore.Api.Services.Interfaces
{
    public interface IImportService
    {
        Task<(bool Success, string Message, object? Data)> GetAllImportsAsync();
        Task<(bool Success, string Message, object? Data)> GetImportByCodeAsync(string importCode);
        Task<(bool Success, string Message, object? Data)> CreateImportAsync(ImportDto dto, int employeeAccountId);
        Task<(bool Success, string Message)> UpdateImportStatusAsync(int importId, string status);
        Task<(bool Success, string Message)> UpdatePaymentStatusAsync(int importId);
    }
}
