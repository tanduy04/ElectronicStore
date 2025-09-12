using System.ComponentModel.DataAnnotations;

namespace ElectronicStore.Api.Dto
{
    public class LoginDto
    {
        [Required(ErrorMessage = "User không được để trống")]
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
