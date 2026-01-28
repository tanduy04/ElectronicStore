using ElectronicStore.Api.Dto;

namespace ElectronicStore.Api.Services.Interfaces
{
    public interface ICustomerService
    {
        Task<(bool Success, string Message, object? Data)> GetAllCustomersAsync(int pageNumber = 1, int pageSize = 10);
        Task<(bool Success, string Message, object? Data)> GetCustomerByIdAsync(int id, int? requestingAccountId = null, string? role = null);
        Task<(bool Success, string Message, object? Data)> GetCustomerByAccountIdAsync(int accountId);
        Task<(bool Success, string Message, object? Data)> SearchByPhoneAsync(string phone);
        Task<(bool Success, string Message)> UpdateCustomerAsync(int id, CustomerDto dto);
        Task<(bool Success, string Message)> UpdateProfileAsync(int accountId, CustomerProfileDto dto);
    }
}
