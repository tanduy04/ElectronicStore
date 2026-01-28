using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;

        public EmployeeController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll(int pageNumber = 1, int pageSize = 10)
        {
            var result = await _employeeService.GetAllEmployeesAsync(pageNumber, pageSize);

            if (!result.Success)
                return StatusCode(500, result.Message);

            return Ok(result.Data);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _employeeService.GetEmployeeByIdAsync(id);

            if (!result.Success)
                return NotFound(result.Message);

            return Ok(result.Data);
        }
        [HttpGet("search")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SearchByPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return BadRequest("Phone number is required.");

            var result = await _employeeService.SearchEmployeesByPhoneAsync(phone);

            if (!result.Success)
                return NotFound(result.Message);

            return Ok(result.Data);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromForm] EmployeeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _employeeService.UpdateEmployeeAsync(id, dto);

            if (!result.Success)
                return result.Message.Contains("not found") ? NotFound(result.Message) : BadRequest(result.Message);

            return Ok(result.Message);
        }
        [HttpPost]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _employeeService.CreateEmployeeAsync(dto);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }
        //[HttpGet]
        //[Route("CreateAdmin")]
        //public async Task<IActionResult> CreateAdmin()
        //{
        //    try
        //    {
                
                
        //        var account = new Account
        //        {
        //            Username = "admin",
        //            Email = "admin@gmail.com",
        //            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin"),
        //            PhoneNumber = "091199888",
        //            RoleId = 1,
        //            IsActive = true,
        //            Avatar = "default-avatar.jpg",
        //            CreatedAt = DateTime.Now,
        //            UpdatedAt = DateTime.Now
        //        };

        //        _context.Accounts.Add(account);
        //        await _context.SaveChangesAsync();


        //        var employee = new Employee
        //        {
        //            AccountId = account.AccountId,
        //            FullName = "Admin",
        //            BirthDate = DateOnly.Parse("1990-12-09"),
        //            Address = "TPHCM",
        //            Position = "Admin",
        //            Salary = 0,
        //            HireDate = DateOnly.Parse("2000-01-01"),
        //            CreatedAt = DateTime.Now
        //        };

        //        _context.Employees.Add(employee);
        //        await _context.SaveChangesAsync();

        //        return Ok(new { message = "Add new employee success", EmployeeID = employee.EmployeeId });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, "Internal server error: " + ex.Message);
        //    }

        //}
        [HttpPut]
        [Route("EditMyProfile")]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> Update([FromForm] EmployeeProfileDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var accountId = User.FindFirst("AccountID")?.Value;
            if (accountId == null)
                return Unauthorized();

            var result = await _employeeService.UpdateEmployeeProfileAsync(int.Parse(accountId), dto);

            if (!result.Success)
                return result.Message.Contains("not found") ? NotFound(result.Message) : BadRequest(result.Message);

            return Ok(result.Message);
        }

    }
}
