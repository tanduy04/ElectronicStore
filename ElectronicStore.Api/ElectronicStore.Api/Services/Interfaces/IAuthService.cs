using ElectronicStore.Api.Dto;

namespace ElectronicStore.Api.Services.Interfaces
{
    public interface IAuthService
    {
        Task<(bool Success, string Message, object? Data)> RegisterAsync(RegisterDto dto);
        Task<(bool Success, string Message, object? Data)> LoginAsync(LoginDto dto);
        Task<(bool Success, string Message, object? Data)> RefreshTokenAsync(RefreshTokenDto dto);
        Task<(bool Success, string Message)> ChangePasswordAsync(int accountId, ChangePasswordDto dto);
        Task<(bool Success, string Message)> ForgotPasswordAsync(ForgotPasswordDto dto);
        Task<(bool Success, string Message)> ResetPasswordAsync(string token, string newPassword);
        Task<(bool Success, string Message, object? Data)> GetProfileAsync(int accountId, string role);
    }
}
