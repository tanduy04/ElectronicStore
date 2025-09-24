using System.ComponentModel.DataAnnotations;

namespace ElectronicStore.Api.Dto
{
    public class CheckoutCodDto
    {
        [Required(ErrorMessage ="FullName is required")]
        public string FullName { get; set; }
        [Required(ErrorMessage ="PhoneNumber is required")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must be 10 digits starting with 0")]
        public string PhoneNumber { get; set; }
        [Required(ErrorMessage ="Address is required")]
        public string Address { get; set; }
        public string? VoucherCode { get; set; }
    }
}
