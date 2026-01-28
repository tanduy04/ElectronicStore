using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Repositories.Interfaces;
using ElectronicStore.Api.Services.Interfaces;

namespace ElectronicStore.Api.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public CategoryService(
            IUnitOfWork unitOfWork,
            IConfiguration config,
            IWebHostEnvironment env)
        {
            _unitOfWork = unitOfWork;
            _config = config;
            _env = env;
        }

        private string GetBaseUrl() => _config["AppSettings:BaseUrl"];

        public async Task<(bool Success, string Message, object? Data)> GetAllCategoriesAsync()
        {
            try
            {
                var categories = await _unitOfWork.Categories.GetAllAsync();
                var baseUrl = GetBaseUrl();

                var result = categories.Select(c => new
                {
                    c.CategoryId,
                    c.CategoryName,
                    ImageUrl = $"{baseUrl}{_config["ImageSettings:CategoryPath"]}{c.CategoryImage}",
                    c.IsActive
                });

                return (true, "Success", result);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> GetCategoryByIdAsync(int id)
        {
            try
            {
                var category = await _unitOfWork.Categories.GetByIdAsync(id);
                if (category == null)
                    return (false, "Category not found", null);

                var baseUrl = GetBaseUrl();

                var result = new
                {
                    category.CategoryId,
                    category.CategoryName,
                    ImageUrl = $"{baseUrl}{_config["ImageSettings:CategoryPath"]}{category.CategoryImage}",
                    category.IsActive
                };

                return (true, "Success", result);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> SearchCategoriesAsync(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return (false, "Search term is required", null);

                var categories = await _unitOfWork.Categories.FindAsync(
                    c => c.CategoryName.Contains(name));

                var baseUrl = GetBaseUrl();

                var result = categories.Select(c => new
                {
                    c.CategoryId,
                    c.CategoryName,
                    ImageUrl = $"{baseUrl}{_config["ImageSettings:CategoryPath"]}{c.CategoryImage}",
                    c.IsActive
                });

                return (true, "Success", result);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> CreateCategoryAsync(CategoryDto dto)
        {
            try
            {
                var category = new Category
                {
                    CategoryName = dto.CategoryName,
                    CategoryImage = dto.CategoryImage ?? "default-category.jpg",
                    IsActive = true
                };

                await _unitOfWork.Categories.AddAsync(category);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Category created successfully", category);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> UpdateCategoryAsync(int id, CategoryDto dto)
        {
            try
            {
                var category = await _unitOfWork.Categories.GetByIdAsync(id);
                if (category == null)
                    return (false, "Category not found");

                category.CategoryName = dto.CategoryName;
                if (!string.IsNullOrEmpty(dto.CategoryImage))
                    category.CategoryImage = dto.CategoryImage;

                _unitOfWork.Categories.Update(category);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Category updated successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteCategoryAsync(int id)
        {
            try
            {
                var category = await _unitOfWork.Categories.GetByIdAsync(id);
                if (category == null)
                    return (false, "Category not found");

                // Soft delete - set IsActive to false
                category.IsActive = false;
                _unitOfWork.Categories.Update(category);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Category deleted successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }
    }
}
