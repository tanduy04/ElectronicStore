using System.ComponentModel.DataAnnotations;

namespace ElectronicStore.Api.Dto
{
    public class SupplierDto
    {
        [Required]
        public string SupplierName { get; set; }
        [Required]

        public string SupplierPhone { get; set; }
        [Required]

        public string SupplierAddress { get; set; }
    }
}
