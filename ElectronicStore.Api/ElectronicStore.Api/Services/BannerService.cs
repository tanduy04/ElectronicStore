using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Helper;
using ElectronicStore.Api.Repositories.Interfaces;
using ElectronicStore.Api.Services.Interfaces;

namespace ElectronicStore.Api.Services
{
    public class BannerService : IBannerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public BannerService(
            IUnitOfWork unitOfWork,
            IConfiguration config,
            IWebHostEnvironment env)
        {
            _unitOfWork = unitOfWork;
            _config = config;
            _env = env;
        }

        private string GetBaseUrl() => _config["AppSettings:BaseUrl"];

        private string GetBannerFolderPath()
        {
            return Path.Combine(_env.WebRootPath, _config["ImageSettings:BannerPath"]);
        }

        private async Task<string> SaveBannerImageAsync(string bannerName, IFormFile imageFile)
        {
            string folderPath = GetBannerFolderPath();
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string normalized = ImageHelper.NormalizeFileName(bannerName);
            string extension = Path.GetExtension(imageFile.FileName);
            string fileName = $"Banner_{normalized}{extension}";
            string filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return fileName;
        }

        public async Task<(bool Success, string Message, object? Data)> GetAllBannersAsync()
        {
            try
            {
                var banners = await _unitOfWork.Banners.GetAllAsync();
                var baseUrl = GetBaseUrl();

                var result = banners.Select(b => new
                {
                    b.BannerId,
                    b.BannerName,
                    ImageUrl = $"{baseUrl}{_config["ImageSettings:BannerPath"]}{b.ImageUrl}"
                });

                return (true, "Success", result);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> GetBannerByIdAsync(int id)
        {
            try
            {
                var banner = await _unitOfWork.Banners.GetByIdAsync(id);
                if (banner == null)
                    return (false, "Banner not found", null);

                var baseUrl = GetBaseUrl();

                var result = new
                {
                    banner.BannerId,
                    banner.BannerName,
                    ImageUrl = $"{baseUrl}{_config["ImageSettings:BannerPath"]}{banner.ImageUrl}"
                };

                return (true, "Success", result);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> CreateBannerAsync(BannerDto dto, IFormFile? imageFile)
        {
            try
            {
                string? imageFileName = null;

                if (imageFile != null)
                {
                    imageFileName = await SaveBannerImageAsync(dto.BannerName, imageFile);
                }

                var banner = new Banner
                {
                    BannerName = dto.BannerName,
                    ImageUrl = imageFileName ?? "default-banner.jpg"
                };

                await _unitOfWork.Banners.AddAsync(banner);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Banner created successfully", banner);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> UpdateBannerAsync(int id, BannerDto dto, IFormFile? imageFile)
        {
            try
            {
                var banner = await _unitOfWork.Banners.GetByIdAsync(id);
                if (banner == null)
                    return (false, "Banner not found");

                banner.BannerName = dto.BannerName;

                if (imageFile != null)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(banner.ImageUrl) && banner.ImageUrl != "default-banner.jpg")
                    {
                        var oldImagePath = Path.Combine(GetBannerFolderPath(), banner.ImageUrl);
                        if (File.Exists(oldImagePath))
                        {
                            File.Delete(oldImagePath);
                        }
                    }

                    // Save new image
                    banner.ImageUrl = await SaveBannerImageAsync(dto.BannerName, imageFile);
                }

                _unitOfWork.Banners.Update(banner);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Banner updated successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteBannerAsync(int id)
        {
            try
            {
                var banner = await _unitOfWork.Banners.GetByIdAsync(id);
                if (banner == null)
                    return (false, "Banner not found");

                // Delete image file
                if (!string.IsNullOrEmpty(banner.ImageUrl) && banner.ImageUrl != "default-banner.jpg")
                {
                    var imagePath = Path.Combine(GetBannerFolderPath(), banner.ImageUrl);
                    if (File.Exists(imagePath))
                    {
                        File.Delete(imagePath);
                    }
                }

                _unitOfWork.Banners.Remove(banner);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Banner deleted successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }
    }
}
