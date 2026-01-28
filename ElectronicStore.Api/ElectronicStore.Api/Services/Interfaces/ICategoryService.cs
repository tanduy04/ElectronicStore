using ElectronicStore.Api.Dto;

namespace ElectronicStore.Api.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<(bool Success, string Message, object? Data)> GetAllCategoriesAsync();
        Task<(bool Success, string Message, object? Data)> GetCategoryByIdAsync(int id);
        Task<(bool Success, string Message, object? Data)> SearchCategoriesAsync(string name);
        Task<(bool Success, string Message, object? Data)> CreateCategoryAsync(CategoryDto dto);
        Task<(bool Success, string Message)> UpdateCategoryAsync(int id, CategoryDto dto);
        Task<(bool Success, string Message)> DeleteCategoryAsync(int id);
    }
}
