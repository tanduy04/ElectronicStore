using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ElectronicStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ElectronicStoreContext _db;
        private readonly IConfiguration _config;

        public AuthController(IAuthService authService, ElectronicStoreContext db, IConfiguration config)
        {
            _authService = authService;
            _db = db;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.RegisterAsync(dto);
            
            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Message);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.LoginAsync(dto);
            
            if (!result.Success)
                return Unauthorized(result.Message);

            return Ok(result.Data);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.RefreshTokenAsync(dto);
            
            if (!result.Success)
                return Unauthorized(result.Message);

            return Ok(result.Data);
        }
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var accountId = int.Parse(User.FindFirstValue("AccountID"));
            var result = await _authService.ChangePasswordAsync(accountId, model);
            
            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Message);
        }
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var result = await _authService.ForgotPasswordAsync(dto);
            
            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Message);
        }
        [HttpGet("get-my-profile")]
        [Authorize]
        public async Task<IActionResult> GetMyProfile()
        {
            try
            {
                var accountID = int.Parse(User.FindFirst("AccountID").Value);
                var baseUrl = $"{Request.Scheme}://{Request.Host}/";
                if (User.IsInRole("Customer"))
                {
                    var customer = await _db.Customers
                    .Include(c => c.Account)
                    .Where(c => c.AccountId == accountID)
                    .Select(c => new
                    {
                        c.Account.Role.RoleName,
                        c.CustomerId,
                        c.FullName,
                        c.Address,
                        c.Gender,
                        BirthDate = c.BirthDate.HasValue
                            ? c.BirthDate.Value.ToString("dd/MM/yyyy")
                            : null, // format ngày kiểu Việt Nam
                        c.Phone,
                        c.Account.Email,
                        c.Point,
                        c.Account.IsActive,
                        ImageUrl = $"{baseUrl}{_config["ImageSettings:AccountPath"]}{c.Account.Avatar}"
                    })
                    .FirstOrDefaultAsync();

                    if (customer == null)
                        return NotFound("Customer not found.");

                    // Kiểm tra quyền truy cập
                    return Ok(customer);
                }
                else
                {
                    var employee = await _db.Employees
                    .Include(c => c.Account)
                    .ThenInclude(c => c.Role)
                    .Where(c => c.AccountId == accountID)
                    .Select(c => new
                    {
                        c.Account.Role.RoleName,
                        c.EmployeeId,
                        c.FullName,
                        c.Address,
                        c.Position,
                        c.Salary,
                        c.HireDate,
                        c.BirthDate,
                        c.Phone,
                        c.Account.Email,
                        c.Account.IsActive,
                        ImageUrl = $"{baseUrl}{_config["ImageSettings:AccountPath"]}{c.Account.Avatar}",
                    })
                    .FirstOrDefaultAsync();

                    if (employee == null) return NotFound("Employee not found.");
                    return Ok(employee);

                }

            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }
    }
}
