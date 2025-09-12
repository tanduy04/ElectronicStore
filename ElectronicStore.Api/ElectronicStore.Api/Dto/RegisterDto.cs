using System.ComponentModel.DataAnnotations;

namespace ElectronicStore.Api.Dto
{
    public class RegisterDto
    {
        [Required]

        public string Username { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]

        public string Password { get; set; }
        [Required]

        public string FullName { get; set; }
    }
}
