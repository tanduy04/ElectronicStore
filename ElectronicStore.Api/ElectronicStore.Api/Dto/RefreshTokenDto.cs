using System.ComponentModel.DataAnnotations;

namespace ElectronicStore.Api.Dto
{
    public class RefreshTokenDto
    {
        [Required(ErrorMessage = "RefreshToken is required")]
        public string RefreshToken { get; set; }
    }
}
