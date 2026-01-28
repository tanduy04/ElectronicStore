using ElectronicStore.Api.Dto;

namespace ElectronicStore.Api.Services.Interfaces
{
    public interface IOrderService
    {
        Task<(bool Success, string Message, object? Data)> GetAllOrdersAsync(int pageNumber = 1, int pageSize = 10);
        Task<(bool Success, string Message, object? Data)> FilterOrdersAsync(string status, int pageNumber = 1, int pageSize = 10);
        Task<(bool Success, string Message, object? Data)> GetOrderByOrderCodeAsync(string orderCode);
        Task<(bool Success, string Message, object? Data)> GetOrdersByCustomerAccountIdAsync(int accountId);
        Task<(bool Success, string Message)> UpdateOrderStatusAsync(string orderCode, string newStatus);
        Task<(bool Success, string Message)> CancelOrderAsync(string orderCode, string? role = null, int? accountId = null);
        Task<(bool Success, string Message)> RefundOrderAsync(string orderCode);
    }
}
