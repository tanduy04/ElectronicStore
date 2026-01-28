using ElectronicStore.Api.Dto;

namespace ElectronicStore.Api.Services.Interfaces
{
    public interface ICheckoutService
    {
        Task<(bool Success, string Message, object? Data)> CheckVoucherAsync(string voucherCode, int accountId);
        Task<(bool Success, string Message, object? Data)> CheckoutCODAsync(CheckoutCartDto dto, int accountId);
        Task<(bool Success, string Message, object? Data)> CheckoutVNPayAsync(CheckoutCartDto dto, int accountId, string ipAddress);
        Task<(bool Success, string Message, object? Data)> ProcessVNPayCallbackAsync(Dictionary<string, string> vnpayData);
    }
}
