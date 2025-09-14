using System.ComponentModel.DataAnnotations;

namespace ElectronicStore.Api.Dto
{
    public class CategoryDto
    {
        [Required(ErrorMessage = "Category name is required")]
        public string CategoryName { get; set; }
        [Required(ErrorMessage = "Image is required")]
        public IFormFile CategoryImage { get; set; }
        [Required(ErrorMessage = "IsActive is required")]
        public bool IsActive { get; set; }
    }
}
