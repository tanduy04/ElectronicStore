using System.ComponentModel.DataAnnotations;

namespace ElectronicStore.Api.Dto
{
    public class FlashSaleDto
    {

        public string FlashSaleName { get; set; }
        public string Description { get; set; }
        [Required]

        public DateOnly Date { get; set; }
        [Required]
        public TimeOnly StartTime { get; set; }
        [Required]

        public TimeOnly EndTime{ get; set; }

        public List<FlashSaleItemDto> Items { get; set; }
    }

    public class FlashSaleItemDto
    {
        [Required]

        public int ProductId { get; set; }
        [Required]

        public decimal SellPrice { get; set; }
        [Required]

        public int Quantity { get; set; }
    }
    public class FlashSaleItemAddDto
    {
        [Required]

        public int FlashSaleId { get; set; }
        [Required]

        public int ProductId { get; set; }
        [Required]

        public decimal SellPrice { get; set; }
        [Required]

        public int Quantity { get; set; }
    }
    public class FlashSaleEditDto
    {

        public string FlashSaleName { get; set; }
        public string Description { get; set; }
        [Required]

        public DateOnly Date { get; set; }
        [Required]
        public TimeOnly StartTime { get; set; }
        [Required]

        public TimeOnly EndTime { get; set; }

    }
    public class FlashSaleViewDto
    {
        public int FlashSaleId { get; set; }
        public string FlashSaleName { get; set; }
        public string Description { get; set; }
        public DateOnly DateSale { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public List<FlashSaleItemViewDto> Items { get; set; }
    }
    public class FlashSaleItemViewDto
    {
        public int ItemId { get; set; }
        public ProductSaleViewDto Product { get; set; }

        public decimal SellPrice { get; set; }
        public int Quantity { get; set; }
    }
    public class  ProductSaleViewDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal OriginalPrice { get; set; }
        public string? imageUrl { get; set; }
    }
}
