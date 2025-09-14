using System.ComponentModel.DataAnnotations;

namespace ElectronicStore.Api.Dto
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Username is required")]
        [MinLength(8, ErrorMessage = "Username must be at least 8 characters long")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
            ErrorMessage = "Password must be at least 8 characters and include at least one letter, one number, and one special character")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must be 10 digits starting with 0")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        public string FullName { get; set; }
    }
}
