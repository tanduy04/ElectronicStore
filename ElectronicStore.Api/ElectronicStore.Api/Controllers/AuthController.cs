using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Service;
using Google.Apis.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using SendGrid.Helpers.Mail;
using System;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;

namespace ElectronicStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ElectronicStoreContext _db;
        private readonly TokenService _tokenService;
        private readonly IConfiguration _config;
        private readonly EmailService _emailService;

        public AuthController(ElectronicStoreContext db, TokenService tokenService, IConfiguration config, EmailService emailService)
        {
            _db = db;
            _tokenService = tokenService;
            _config = config;
            _emailService = emailService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                if (_db.Accounts.Any(a => a.Email == dto.Email))
                    return BadRequest("Email already exists");
                if (_db.Customers.Any(a => a.Phone == dto.PhoneNumber && a.AccountId != null))
                    return BadRequest("Phone number already exists");
                var role_custommer = _db.Roles.FirstOrDefault(r => r.RoleName == "Customer");
                var newAccount = new Account
                {
                    Email = dto.Email,
                    Username = dto.Username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    RoleId = role_custommer.RoleId,
                    IsActive = true,
                    LoginType= "Local",
                   Avatar = "default-avatar.jpg",
                    CreatedAt = DateTime.Now,
                };
                _db.Accounts.Add(newAccount);
                await _db.SaveChangesAsync();
                var account = _db.Accounts.First(a => a.Username == newAccount.Username);
                var custommerExist = _db.Customers.FirstOrDefault(a => a.Phone == dto.PhoneNumber && a.AccountId == null);
                if (custommerExist != null)
                {
                    custommerExist.FullName = dto.FullName;
                    custommerExist.AccountId = account.AccountId;
                    _db.Customers.Update(custommerExist);
                    await _db.SaveChangesAsync();
                }    
                var custommer = new Customer
                {
                    FullName = dto.FullName,
                    AccountId = account.AccountId,
                    Phone = dto.PhoneNumber,
                    CreatedAt = DateTime.Now,
                    Point = 0
                };

                _db.Customers.Add(custommer);
                await _db.SaveChangesAsync();



                return Ok("Registered successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                var account = _db.Accounts.Include(a => a.Role).FirstOrDefault(a => a.Username == dto.Username);
                if (account == null || !BCrypt.Net.BCrypt.Verify(dto.Password, account.PasswordHash))
                    return Unauthorized("Incorrect username or password");
                if (!account.IsActive)
                    return Unauthorized("Account is deactivated");

                var accessToken = _tokenService.GenerateAccessToken(account);
                var refreshToken = _tokenService.GenerateRefreshToken();

                // Lưu refresh token vào db
                _db.AccountTokens.Add(new AccountToken
                {
                    AccountId = account.AccountId,
                    RefreshToken = refreshToken,
                    ExpiryDate = DateTime.Now.AddDays(int.Parse(_config["JwtSettings:RefreshTokenExpirationDays"]))
                });
                await _db.SaveChangesAsync();

                return Ok(new { accessToken, refreshToken });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }

        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                var token = _db.AccountTokens.FirstOrDefault(t => t.RefreshToken == dto.RefreshToken);
                if (token == null || token.ExpiryDate < DateTime.Now)
                    return Unauthorized("Invalid refresh token");

                var account = _db.Accounts.Include(a => a.Role).First(a => a.AccountId == token.AccountId);
                var newAccessToken = _tokenService.GenerateAccessToken(account);
                var newRefreshToken = _tokenService.GenerateRefreshToken();

                // Update token in DB
                token.RefreshToken = newRefreshToken;
                token.ExpiryDate = DateTime.Now.AddDays(int.Parse(_config["JwtSettings:RefreshTokenExpirationDays"]));
                await _db.SaveChangesAsync();

                return Ok(new { accessToken = newAccessToken, refreshToken = newRefreshToken });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                var accountId = int.Parse(User.FindFirstValue("AccountID"));

                var account = await _db.Accounts.FirstOrDefaultAsync(x => x.AccountId == accountId);
                if (account == null)
                    return NotFound("Account not found!");

                if (!BCrypt.Net.BCrypt.Verify(model.OldPassword, account.PasswordHash))
                    return BadRequest("Incorrect old password");

                account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                await _db.SaveChangesAsync();

                return Ok("Password changed successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            try
            {
                var user = await _db.Accounts.FirstOrDefaultAsync(u => u.Email == dto.Email);
                if (user == null)
                {
                    return BadRequest("Email doesn't exist");
                }

                var newPassword = GenerateRandomPassword(10);

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                await _db.SaveChangesAsync();
                await _emailService.SendForgotPasswordEmail(dto.Email,user.Username, newPassword);

                return Ok("A new password has been sent to your email.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
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
                    .Where(c => c.AccountId == accountID)
                    .Select(c => new
                    {
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

        // Hàm tạo mật khẩu ngẫu nhiên
        private string GenerateRandomPassword(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

    }
}
