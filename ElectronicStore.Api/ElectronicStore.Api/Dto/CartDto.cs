using System.ComponentModel.DataAnnotations;

namespace ElectronicStore.Api.Dto
{
    public class AddToCartDto
    {
        [Required]
        public int ProductId { get; set; }
        [Range(0, int.MaxValue)]

        public int Quantity { get; set; } = 1;
    }

    public class UpdateCartDto
    {
        [Required]

        public int ProductId { get; set; }
        [Required]
        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }
    }

}
