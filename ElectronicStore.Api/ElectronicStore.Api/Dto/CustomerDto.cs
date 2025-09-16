using System.ComponentModel.DataAnnotations;

namespace ElectronicStore.Api.Dto
{
    public class CustomerDto
    {
        [Required]
        public string Email { get; set; }
        [Required]

        public string PhoneNumber { get; set; }
        [Required]

        public bool IsActive { get; set; } = true;

        public string? Address { get; set; }

        public string? FullName { get; set; }
    }
    public class CustomerProfileDto
    {
        [Required]

        public string Email { get; set; }
        [Required]

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
