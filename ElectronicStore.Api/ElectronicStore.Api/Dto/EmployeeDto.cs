using System.ComponentModel.DataAnnotations;

namespace ElectronicStore.Api.Dto
{
    public class CreateEmployeeDto
    {
        [Required(ErrorMessage = "Full name is required")]
        public string FullName { get; set; }
        [Required(ErrorMessage = "Birth date is required")]
        public DateOnly BirthDate { get; set; }
        public string? Address { get; set; }
        [Required(ErrorMessage = "Position is required")]
        public string Position { get; set; }
        [Required(ErrorMessage = "Salary is required")]
        public decimal Salary { get; set; }
        [Required(ErrorMessage = "Hire date is required")]
        public DateOnly HireDate { get; set; }
        public bool IsActive { get; set; } = true;


        // Thông tin account
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; }
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must be 10 digits starting with 0")]
        public string PhoneNumber { get; set; }
    }
    public class  EmployeeDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must be 10 digits starting with 0")]
        public string PhoneNumber { get; set; }
        [Required(ErrorMessage ="Position is required")]
        public string Position { get; set; }
        [Required(ErrorMessage ="Salary is required")]
        public decimal Salary { get; set; }
        [Required]
        public DateOnly HireDate { get; set; }

        public bool IsActive { get; set; } = true;
        public string? Address { get; set; }

        public string? FullName { get; set; }
    
    }
    public class EmployeeProfileDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must be 10 digits starting with 0")]
        public string PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? FullName { get; set; }
        public IFormFile? Avatar { get; set; }
        public DateOnly? BirthDate { get; set; }
    }
    }
