using ElectronicStore.Api.Dto;

namespace ElectronicStore.Api.Services.Interfaces
{
    public interface IBrandService
    {
        Task<(bool Success, string Message, object? Data)> GetAllBrandsAsync();
        Task<(bool Success, string Message, object? Data)> GetBrandByIdAsync(int id);
        Task<(bool Success, string Message, object? Data)> SearchBrandsAsync(string name);
        Task<(bool Success, string Message, object? Data)> CreateBrandAsync(BrandDto dto);
        Task<(bool Success, string Message)> UpdateBrandAsync(int id, BrandDto dto);
        Task<(bool Success, string Message)> DeleteBrandAsync(int id);
    }
}
