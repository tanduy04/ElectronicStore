using System.ComponentModel.DataAnnotations;

namespace ElectronicStore.Api.Dto
{
    public class VoucherDto
    {
        [Required]
        public string VoucherCode { get; set; } = null!;
        [Required]

        public string? VoucherName { get; set; }
        [Required]

        public string? DiscountType { get; set; }
        [Required]

        public decimal DiscountValue { get; set; }
        [Required]

        public int? Quantity { get; set; }
        [Required]

        public DateTime? StartDate { get; set; }
        [Required]

        public DateTime? EndDate { get; set; }
        [Required]

        public bool IsActive { get; set; } 
    }
}
