using ElectronicStore.Api.Dto;

namespace ElectronicStore.Api.Services.Interfaces
{
    public interface ICartService
    {
        Task<(bool Success, string Message, object? Data)> GetCartByAccountIdAsync(int accountId);
        Task<(bool Success, string Message)> AddToCartAsync(int accountId, AddToCartDto dto);
        Task<(bool Success, string Message)> UpdateCartItemAsync(int accountId, int productId, int quantity);
        Task<(bool Success, string Message)> RemoveFromCartAsync(int accountId, int productId);
        Task<(bool Success, string Message)> ClearCartAsync(int accountId);
    }
}
