using System.ComponentModel.DataAnnotations;

namespace ElectronicStore.Api.Dto
{
    public class BrandDto
    {
        [Required(ErrorMessage ="BrandName is required")]
        public string BrandName { get; set; }

        public IFormFile? BrandImage { get; set; }
        public bool IsActive { get; set; } = true;
    }

}
