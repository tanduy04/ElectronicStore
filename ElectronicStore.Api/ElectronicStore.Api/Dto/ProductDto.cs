using System.ComponentModel.DataAnnotations;

namespace ElectronicStore.Api.Dto
{
    public class ProductDto
    {
        [Required(ErrorMessage = "ProductName is required")]
        public string ProductName { get; set; }
        [Required(ErrorMessage = "ConsumptionCapacity is required")]

        public string Description { get; set; }
        [Required(ErrorMessage = "ConsumptionCapacity is required")]

        public int ConsumptionCapacity { get; set; }
        [Required(ErrorMessage = "Maintenance is required")]

        public int Maintenance { get; set; }
        [Required(ErrorMessage = "Price is required")]

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }
        [Required(ErrorMessage = "StockQuantity is required")]

        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }
        [Required(ErrorMessage = "CategoryID is required")]

        public int CategoryID { get; set; }
        [Required(ErrorMessage = "BrandID is required")]

        public int BrandID { get; set; }
        [Required(ErrorMessage = "ManufactureYear is required")]

        public int ManufactureYear { get; set; }
        public bool IsActive { get; set; }

        // Images
        [Required(ErrorMessage = "MainImage is required")]

        public IFormFile? MainImage { get; set; }
        [Required(ErrorMessage = "SubImages is required")]

        public List<IFormFile>? SubImages { get; set; }       // nhiều ảnh phụ (optional)
    }

}
