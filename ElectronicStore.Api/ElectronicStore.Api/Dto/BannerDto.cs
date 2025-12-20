using System.ComponentModel.DataAnnotations;

namespace ElectronicStore.Api.Dto
{
    public class BannerDto
    {
        [Required(ErrorMessage = "BannerName ís required")]
        public string? BannerName { get; set; }
        public IFormFile? ImageFile { get; set; }  // Có thể null khi update
    }

}
