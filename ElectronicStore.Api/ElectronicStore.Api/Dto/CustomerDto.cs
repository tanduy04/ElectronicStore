using System.ComponentModel.DataAnnotations;

namespace ElectronicStore.Api.Dto
{
    public class CustomerDto
    {
        [Required]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }
        [Required]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must be 10 digits starting with 0")]
        public string PhoneNumber { get; set; }
        [Required]

        public bool IsActive { get; set; } = true;

        public string? Address { get; set; }

        public string? FullName { get; set; }
    }
    public class CustomerProfileDto
    {
        [Required]
        [EmailAddress(ErrorMessage = "Invalid email address")]

        public string Email { get; set; }
        [Required]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must be 10 digits starting with 0")]

        public string PhoneNumber { get; set; }
        [Required]

        public string? Address { get; set; }
        [Required]

        public string? FullName { get; set; }
        [Required]

        public IFormFile? Avatar { get; set; }
        [Required]

        public DateOnly? BirthDate { get; set; }
        [Required]

        public byte? Gender { get; set; }
    }
}
