using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace ElectronicStore.WebApi.ViewModel
{
    public class RefreshTokenModel
    {
        [Required]
        public string RefreshToken { get; set; }
    }
}
