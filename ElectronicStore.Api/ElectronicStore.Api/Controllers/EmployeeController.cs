using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly ElectronicStoreContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public EmployeeController(ElectronicStoreContext context, IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _env = env;
            _config = config;
        }
        
        private string GetFolder()
        {
            var relative = _config["AccountPath:AccountPath"] ?? "Image/AvatarAccount/";
            return Path.Combine(_env.WebRootPath ?? "wwwroot", relative);
        }

        private string GetBaseUrl() => $"{Request.Scheme}://{Request.Host}/";

        private object MapEmployeeToDto(Employee c)
        {
            return new
            {
                c.EmployeeId,
                c.FullName,
                c.Address,
                c.Position,
                c.Salary,
                c.HireDate,
                c.BirthDate,
                c.Account.PhoneNumber,
                c.Account.Email,
                c.Account.IsActive,
                c.Account.Avatar,
            };
        }
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll(int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var query = _context.Employees
                .Include(c => c.Account)
                .OrderByDescending(c => c.EmployeeId);

                var totalItems = await query.CountAsync();

                var employees = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                var result = employees.Select(MapEmployeeToDto);
                return Ok(new
                {
                    TotalItems = totalItems,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        // GET: api/customers/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var employee = await _context.Employees
                .Include(c => c.Account)
                .Where(c => c.EmployeeId == id)
                .Select(c => new
                {
                    c.EmployeeId,
                    c.FullName,
                    c.Address,
                    c.Position,
                    c.Salary,
                    c.HireDate,
                    c.BirthDate,
                    c.Account.PhoneNumber,
                    c.Account.Email,
                    c.Account.IsActive,
                })
                .FirstOrDefaultAsync();

                if (employee == null) return NotFound("Employee not found.");
                return Ok(employee);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }
        // GET: api/customers/search?phone=0123456789
        [HttpGet("search")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SearchByPhone(string phone)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(phone))
                    return BadRequest("Phone number is required.");

                var employees = await _context.Employees
                    .Include(c => c.Account)
                    .Where(c => c.Account.PhoneNumber == phone)
                    .ToListAsync();

                if (!employees.Any()) return NotFound("No employees found with this phone number.");

                return Ok(employees.Select(MapEmployeeToDto));
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        // PUT: api/customers/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromForm] EmployeeDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                if (_context.Accounts.Any(a => a.Email == dto.Email))
                    return BadRequest("Email already exists");
                if (_context.Accounts.Any(a => a.PhoneNumber == dto.PhoneNumber))
                    return BadRequest("Phone number already exists");
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null) return NotFound("Employee not found.");
                var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == employee.AccountId);
                employee.FullName = dto.FullName;
                employee.Address = dto.Address;
                employee.Position = dto.Position;
                employee.Salary = dto.Salary;
                employee.HireDate = dto.HireDate;
                account.IsActive = dto.IsActive;
                account.UpdatedAt = DateTime.Now;
                account.Email = dto.Email;
                account.PhoneNumber = dto.PhoneNumber;

                _context.Accounts.Update(account);
                _context.Employees.Update(employee);
                await _context.SaveChangesAsync();

                return Ok("Employee updated successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                if (!ModelState.IsValid) return BadRequest(ModelState);
                if (_context.Accounts.Any(a => a.Email == dto.Email))
                    return BadRequest("Email already exists");
                if (_context.Accounts.Any(a => a.PhoneNumber == dto.PhoneNumber))
                    return BadRequest("Phone number already exists");
                var role = await _context.Roles.FirstOrDefaultAsync(s => s.RoleName == "Employee");
                if (role == null)
                    return BadRequest("Role 'Employee' not found.");
                int roleId = role.RoleId;
                var account = new Account
                {
                    Username = dto.Username,
                    Email = dto.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Username),
                    PhoneNumber = dto.PhoneNumber,
                    RoleId = roleId,
                    IsActive = true,
                    Avatar = "default-avatar.jpg",
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
                    CreatedAt = DateTime.Now
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Add new employee success", EmployeeID = employee.EmployeeId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }

        }
        [HttpPut]
        [Route("EditMyProfile")]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> Update([FromForm] EmployeeProfileDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                if (_context.Accounts.Any(a => a.Email == dto.Email))
                    return BadRequest("Email already exists");
                if (_context.Accounts.Any(a => a.PhoneNumber == dto.PhoneNumber))
                    return BadRequest("Phone number already exists");
                var employee = await _context.Employees.FindAsync(User.IsInRole);
                if (employee == null) return NotFound("Employee not found.");
                var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == employee.AccountId);
                employee.FullName = dto.FullName;
                employee.Address = dto.Address;
                employee.BirthDate = dto.BirthDate;
                account.Email = dto.Email;
                account.PhoneNumber = dto.PhoneNumber;
                if (dto.Avatar != null)
                {
                    if (!ImageHelper.IsImageFile(dto.Avatar))
                        return BadRequest("Please upload a valid image file.");

                    var folder = GetFolder();
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                    // Xóa avatar cũ
                    if (!string.IsNullOrEmpty(account.Avatar) && account.Avatar != "default-avatar.jpg")
                    {
                        var oldPath = Path.Combine(folder, account.Avatar);
                        ImageHelper.DeleteFileIfExists(oldPath, account.Avatar);
                    }

                    // Lưu avatar mới
                    var ext = Path.GetExtension(dto.Avatar.FileName);
                    var avatarFile = $"{Guid.NewGuid().ToString()}{ext}";
                    var fullPath = Path.Combine(folder, avatarFile);

                    using (var fs = new FileStream(fullPath, FileMode.Create))
                    {
                        await dto.Avatar.CopyToAsync(fs);
                    }

                    account.Avatar = avatarFile;
                }

                _context.Employees.Update(employee);
                await _context.SaveChangesAsync();

                return Ok("Employee updated successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

    }
}
