using System.ComponentModel.DataAnnotations;

namespace ElectronicStore.Api.Dto
{
    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }
    }
}
