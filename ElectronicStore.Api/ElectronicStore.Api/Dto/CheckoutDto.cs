using System.ComponentModel.DataAnnotations;

namespace ElectronicStore.Api.Dto
{
    public class CheckoutCartDto
    {
        [Required(ErrorMessage ="FullName is required")]
        public string FullName { get; set; }
        [Required(ErrorMessage ="PhoneNumber is required")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must be 10 digits starting with 0")]
        public string PhoneNumber { get; set; }
        [Required(ErrorMessage ="Address is required")]
        public string Address { get; set; }
        public string? VoucherCode { get; set; }
        public bool? usePoint { get; set; } = false;
    }
    public class CheckoutProductDto
    {
        [Required(ErrorMessage = "FullName is required")]
        public string FullName { get; set; }
        [Required(ErrorMessage = "PhoneNumber is required")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must be 10 digits starting with 0")]
        public string PhoneNumber { get; set; }
        [Required(ErrorMessage = "Address is required")]
        public string Address { get; set; }
        [Required]
        public int ProductId { get; set; }
        [Required]
        public int Quantity { get; set; }
        public string? VoucherCode { get; set; }
        public bool? usePoint { get; set; } = false;
        [Required]
        [RegularExpression("^(VNPAY|COD)$", ErrorMessage = "Payment method must be 'VNPAY' or 'COD'")]
        public string method { get; set; }
    }
}
