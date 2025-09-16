using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BannerController : ControllerBase
    {
        private readonly ElectronicStoreContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public BannerController(ElectronicStoreContext context, IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _env = env;
            _config = config;
        }

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

            return $"{_config["ImageSettings:BannerPath"]}{fileName}";
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}/";
                var banners = await _context.Banners
                    .Select(b => new
                    {
                        b.BannerId,
                        b.BannerName,
                        ImageUrl = string.IsNullOrEmpty(b.ImageUrl) ? null : baseUrl + b.ImageUrl
                    })
                    .ToListAsync();

                return Ok(banners);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}/";
                var banner = await _context.Banners
                    .Where(b => b.BannerId == id)
                    .Select(b => new
                    {
                        b.BannerId,
                        b.BannerName,
                        ImageUrl = string.IsNullOrEmpty(b.ImageUrl) ? null : baseUrl + b.ImageUrl
                    })
                    .FirstOrDefaultAsync();

                if (banner == null)
                    return NotFound("Banner not found.");

                return Ok(banner);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] BannerDto dto)
        {
            try
            {
                if (dto.ImageFile == null || !ImageHelper.IsImageFile(dto.ImageFile))
                    return BadRequest("Please upload a valid image file (jpg, jpeg, png, gif).");

                string imageUrl = await SaveBannerImageAsync(dto.BannerName, dto.ImageFile);

                var banner = new Banner
                {
                    BannerName = dto.BannerName,
                    ImageUrl = imageUrl
                };

                _context.Banners.Add(banner);
                await _context.SaveChangesAsync();

                return Ok(banner);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] BannerDto dto)
        {
            try
            {
                var banner = await _context.Banners.FindAsync(id);
                if (banner == null)
                    return NotFound("Banner not found.");

                banner.BannerName = dto.BannerName;

                if (dto.ImageFile != null)
                {
                    if (!ImageHelper.IsImageFile(dto.ImageFile))
                        return BadRequest("Please upload a valid image file (jpg, jpeg, png, gif).");

                    // Delete old image
                    if (!string.IsNullOrEmpty(banner.ImageUrl))
                    {
                        string oldFolder = GetBannerFolderPath();
                        string oldFileName = Path.GetFileName(banner.ImageUrl);
                        ImageHelper.DeleteFileIfExists(oldFolder, oldFileName);
                    }

                    // Save new image
                    banner.ImageUrl = await SaveBannerImageAsync(dto.BannerName, dto.ImageFile);
                }

                _context.Banners.Update(banner);
                await _context.SaveChangesAsync();

                return Ok(banner);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var banner = await _context.Banners.FindAsync(id);
                if (banner == null)
                    return NotFound("Banner not found.");

                if (!string.IsNullOrEmpty(banner.ImageUrl))
                {
                    string folderPath = GetBannerFolderPath();
                    string fileName = Path.GetFileName(banner.ImageUrl);
                    ImageHelper.DeleteFileIfExists(folderPath, fileName);
                }

                _context.Banners.Remove(banner);
                await _context.SaveChangesAsync();

                return Ok("Banner deleted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }
    }
}
