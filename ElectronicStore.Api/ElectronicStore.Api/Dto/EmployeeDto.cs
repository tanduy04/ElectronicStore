namespace ElectronicStore.Api.Dto
{
    public class CreateEmployeeDto
    {
        public string FullName { get; set; }
        public DateOnly BirthDate { get; set; }
        public string Address { get; set; }
        public string Position { get; set; }
        public decimal Salary { get; set; }
        public DateOnly HireDate { get; set; }
        public bool IsActive { get; set; } = true;

        // Thông tin account
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string PhoneNumber { get; set; }
    }

}
