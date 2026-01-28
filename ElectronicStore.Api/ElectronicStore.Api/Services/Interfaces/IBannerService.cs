using ElectronicStore.Api.Dto;
using Microsoft.AspNetCore.Http;

namespace ElectronicStore.Api.Services.Interfaces
{
    public interface IBannerService
    {
        Task<(bool Success, string Message, object? Data)> GetAllBannersAsync();
        Task<(bool Success, string Message, object? Data)> GetBannerByIdAsync(int id);
        Task<(bool Success, string Message, object? Data)> CreateBannerAsync(BannerDto dto, IFormFile? imageFile);
        Task<(bool Success, string Message)> UpdateBannerAsync(int id, BannerDto dto, IFormFile? imageFile);
        Task<(bool Success, string Message)> DeleteBannerAsync(int id);
    }
}
