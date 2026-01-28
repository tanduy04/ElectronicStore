using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Repositories.Interfaces;
using ElectronicStore.Api.Services.Interfaces;

namespace ElectronicStore.Api.Services
{
    public class BrandService : IBrandService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public BrandService(
            IUnitOfWork unitOfWork,
            IConfiguration config,
            IWebHostEnvironment env)
        {
            _unitOfWork = unitOfWork;
            _config = config;
            _env = env;
        }

        private string GetBaseUrl() => _config["AppSettings:BaseUrl"];

        public async Task<(bool Success, string Message, object? Data)> GetAllBrandsAsync()
        {
            try
            {
                var brands = await _unitOfWork.Brands.GetAllAsync();
                var baseUrl = GetBaseUrl();

                var result = brands.Select(b => new
                {
                    b.BrandId,
                    b.BrandName,
                    ImageUrl = $"{baseUrl}{_config["ImageSettings:BrandPath"]}{b.BrandImage}",
                    b.IsActive
                });

                return (true, "Success", result);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> GetBrandByIdAsync(int id)
        {
            try
            {
                var brand = await _unitOfWork.Brands.GetByIdAsync(id);
                if (brand == null)
                    return (false, "Brand not found", null);

                var baseUrl = GetBaseUrl();

                var result = new
                {
                    brand.BrandId,
                    brand.BrandName,
                    ImageUrl = $"{baseUrl}{_config["ImageSettings:BrandPath"]}{brand.BrandImage}",
                    brand.IsActive
                };

                return (true, "Success", result);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> SearchBrandsAsync(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return (false, "Search term is required", null);

                var brands = await _unitOfWork.Brands.FindAsync(
                    b => b.BrandName.Contains(name));

                var baseUrl = GetBaseUrl();

                var result = brands.Select(b => new
                {
                    b.BrandId,
                    b.BrandName,
                    ImageUrl = $"{baseUrl}{_config["ImageSettings:BrandPath"]}{b.BrandImage}",
                    b.IsActive
                });

                return (true, "Success", result);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> CreateBrandAsync(BrandDto dto)
        {
            try
            {
                var brand = new Brand
                {
                    BrandName = dto.BrandName,
                    BrandImage = dto.BrandImage ?? "default-brand.jpg",
                    IsActive = true
                };

                await _unitOfWork.Brands.AddAsync(brand);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Brand created successfully", brand);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> UpdateBrandAsync(int id, BrandDto dto)
        {
            try
            {
                var brand = await _unitOfWork.Brands.GetByIdAsync(id);
                if (brand == null)
                    return (false, "Brand not found");

                brand.BrandName = dto.BrandName;
                if (!string.IsNullOrEmpty(dto.BrandImage))
                    brand.BrandImage = dto.BrandImage;

                _unitOfWork.Brands.Update(brand);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Brand updated successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteBrandAsync(int id)
        {
            try
            {
                var brand = await _unitOfWork.Brands.GetByIdAsync(id);
                if (brand == null)
                    return (false, "Brand not found");

                // Soft delete
                brand.IsActive = false;
                _unitOfWork.Brands.Update(brand);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Brand deleted successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }
    }
}
