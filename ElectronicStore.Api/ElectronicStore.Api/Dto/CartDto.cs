using System.ComponentModel.DataAnnotations;

namespace ElectronicStore.Api.Dto
{
    public class AddToCartDto
    {
        [Required]
        public int ProductId { get; set; }
        [Required]

        public int Quantity { get; set; } = 1;
    }

    public class UpdateCartDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

}
