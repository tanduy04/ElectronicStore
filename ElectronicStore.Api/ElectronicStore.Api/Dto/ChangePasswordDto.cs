using System.ComponentModel.DataAnnotations;

namespace ElectronicStore.Api.Dto
{
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "password is required")]
        public string OldPassword { get; set; }
        [Required(ErrorMessage = "password is required")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
            ErrorMessage = "Password must be at least 8 characters and include at least one letter, one number, and one special character")]
        public string NewPassword { get; set; }
    }

}
