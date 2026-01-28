using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Helper;
using ElectronicStore.Api.Repositories.Interfaces;
using ElectronicStore.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStore.Api.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly ElectronicStoreContext _context;

        public EmployeeService(
            IUnitOfWork unitOfWork,
            IConfiguration config,
            IWebHostEnvironment env,
            ElectronicStoreContext context)
        {
            _unitOfWork = unitOfWork;
            _config = config;
            _env = env;
            _context = context;
        }

        private string GetBaseUrl() => _config["AppSettings:BaseUrl"];

        private string GetFolder()
        {
            var relative = _config["AccountPath:AccountPath"] ?? "Image/AvatarAccount/";
            return Path.Combine(_env.WebRootPath ?? "wwwroot", relative);
        }

        private object MapEmployeeToDto(Employee employee)
        {
            var baseUrl = GetBaseUrl();
            return new
            {
                employee.EmployeeId,
                employee.FullName,
                employee.Address,
                employee.Position,
                employee.Salary,
                employee.HireDate,
                employee.BirthDate,
                employee.Phone,
                employee.Account.Email,
                employee.Account.IsActive,
                ImageUrl = $"{baseUrl}{_config["ImageSettings:AccountPath"]}{employee.Account.Avatar}"
            };
        }

        public async Task<(bool Success, string Message, object? Data)> GetAllEmployeesAsync(int pageNumber, int pageSize)
        {
            try
            {
                var query = _context.Employees
                    .Include(e => e.Account)
                    .OrderByDescending(e => e.EmployeeId);

                var totalItems = await query.CountAsync();

                var employees = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var result = new
                {
                    TotalItems = totalItems,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                    Data = employees.Select(MapEmployeeToDto)
                };

                return (true, "Success", result);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> GetEmployeeByIdAsync(int id)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.Account)
                    .FirstOrDefaultAsync(e => e.EmployeeId == id);

                if (employee == null)
                    return (false, "Employee not found", null);

                var result = MapEmployeeToDto(employee);
                return (true, "Success", result);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> GetEmployeeByAccountIdAsync(int accountId)
        {
            try
            {
                var employee = await _unitOfWork.Employees.GetByAccountIdAsync(accountId);
                if (employee == null)
                    return (false, "Employee not found", null);

                return (true, "Success", employee);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> CreateEmployeeAsync(EmployeeDto dto, IFormFile? avatarFile)
        {
            try
            {
                // Check if email already exists
                if (await _unitOfWork.Accounts.AnyAsync(a => a.Email == dto.Email))
                    return (false, "Email already exists", null);

                // Create account
                var account = new Account
                {
                    Email = dto.Email,
                    Username = dto.Username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    RoleId = dto.RoleId,
                    IsActive = true,
                    LoginType = "Local",
                    Avatar = "default-avatar.jpg",
                    CreatedAt = DateTime.Now
                };

                await _unitOfWork.Accounts.AddAsync(account);
                await _unitOfWork.SaveChangesAsync();

                // Create employee
                var employee = new Employee
                {
                    FullName = dto.FullName,
                    Address = dto.Address,
                    Position = dto.Position,
                    Salary = dto.Salary,
                    HireDate = dto.HireDate,
                    BirthDate = dto.BirthDate,
                    Phone = dto.Phone,
                    AccountId = account.AccountId
                };

                await _unitOfWork.Employees.AddAsync(employee);
                await _unitOfWork.SaveChangesAsync();

                // Handle avatar upload if provided
                if (avatarFile != null && ImageHelper.IsImageFile(avatarFile))
                {
                    string folderPath = GetFolder();
                    string fileName = await ImageHelper.SaveImageAsync(avatarFile, folderPath, account.AccountId.ToString());
                    account.Avatar = fileName;
                    _unitOfWork.Accounts.Update(account);
                    await _unitOfWork.SaveChangesAsync();
                }

                return (true, "Employee created successfully", employee);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> UpdateEmployeeAsync(int id, EmployeeDto dto, IFormFile? avatarFile)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.Account)
                    .FirstOrDefaultAsync(e => e.EmployeeId == id);

                if (employee == null)
                    return (false, "Employee not found");

                // Update employee info
                employee.FullName = dto.FullName;
                employee.Address = dto.Address;
                employee.Position = dto.Position;
                employee.Salary = dto.Salary;
                employee.HireDate = dto.HireDate;
                employee.BirthDate = dto.BirthDate;
                employee.Phone = dto.Phone;

                // Update account info
                employee.Account.Email = dto.Email;
                employee.Account.IsActive = dto.IsActive;

                // Handle avatar upload
                if (avatarFile != null && ImageHelper.IsImageFile(avatarFile))
                {
                    string folderPath = GetFolder();
                    
                    // Delete old avatar if not default
                    if (employee.Account.Avatar != "default-avatar.jpg")
                    {
                        ImageHelper.DeleteFileIfExists(folderPath, employee.Account.Avatar);
                    }

                    // Save new avatar
                    string fileName = await ImageHelper.SaveImageAsync(avatarFile, folderPath, employee.Account.AccountId.ToString());
                    employee.Account.Avatar = fileName;
                }

                _unitOfWork.Employees.Update(employee);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Employee updated successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteEmployeeAsync(int id)
        {
            try
            {
                var employee = await _unitOfWork.Employees.GetByIdAsync(id);
                if (employee == null)
                    return (false, "Employee not found");

                // Soft delete - set account as inactive
                var account = await _unitOfWork.Accounts.GetByIdAsync(employee.AccountId.Value);
                if (account != null)
                {
                    account.IsActive = false;
                    _unitOfWork.Accounts.Update(account);
                    await _unitOfWork.SaveChangesAsync();
                }

                return (true, "Employee deleted successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }
    }
}
