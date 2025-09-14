using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly ElectronicStoreContext _context;

        public EmployeeController(ElectronicStoreContext context)
        {
            _context = context;
        }
        [HttpPost]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var account = new Account
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password), 
                PhoneNumber = dto.PhoneNumber,
                RoleId = 1,
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync(); 

            
            var employee = new Employee
            {
                AccountId = account.AccountId,
                FullName = dto.FullName,
                BirthDate = dto.BirthDate,
                Address = dto.Address,
                Position = dto.Position,
                Salary = dto.Salary,
                HireDate = dto.HireDate,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.Now
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Nhân viên tạo thành công", EmployeeID = employee.EmployeeId });
        }

    }
}
