using ElectronicStore.Api.Dto;

namespace ElectronicStore.Api.Services.Interfaces
{
    public interface IFlashSaleService
    {
        Task<(bool Success, string Message, object? Data)> GetAllFlashSalesAsync();
        Task<(bool Success, string Message, object? Data)> GetFlashSaleByIdAsync(int id);
        Task<(bool Success, string Message, object? Data)> GetFlashSaleTodayAndTomorrowAsync();
        Task<(bool Success, string Message)> CreateFlashSaleAsync(FlashSaleDto dto);
        Task<(bool Success, string Message)> AddFlashSaleItemAsync(FlashSaleItemAddDto dto);
        Task<(bool Success, string Message)> UpdateFlashSaleAsync(int id, FlashSaleEditDto dto);
        Task<(bool Success, string Message)> UpdateFlashSaleItemAsync(int id, FlashSaleItemDto dto);
        Task<(bool Success, string Message)> DeleteFlashSaleAsync(int id);
        Task<(bool Success, string Message)> DeleteFlashSaleItemAsync(int id);
        Task<(bool Success, string Message, object? Data)> GetFlashSalePriceAsync(int productId, int quantity);
    }
}
