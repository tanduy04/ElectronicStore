using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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

        public AuthController(ElectronicStoreContext db, TokenService tokenService, IConfiguration config, EmailService emailService )
        {
            _db = db;
            _tokenService = tokenService;
            _config = config;
            _emailService = emailService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody]  RegisterDto dto)
        {
            try
            {
                if (_db.Accounts.Any(a => a.Email == dto.Email))
                    return BadRequest("Email already exists");
                if (_db.Accounts.Any(a => a.PhoneNumber == dto.PhoneNumber))
                    return BadRequest("Phone number already exists");
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                var role_custommer = _db.Roles.FirstOrDefault(r => r.RoleName == "Customer");

                var account = new Account
                {
                    Email = dto.Email,
                    Username = dto.Username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    RoleId = role_custommer.RoleId,
                    PhoneNumber = dto.PhoneNumber,
                    IsActive = true,
                    Avatar = "default-avatar.jpg",
                    CreatedAt = DateTime.UtcNow,
                };
                var custommer = new Customer
                {
                    FullName = dto.FullName,
                    AccountId = account.AccountId,
                    CreatedAt = DateTime.UtcNow,
                    Point = 0
                };

                _db.Accounts.Add(account);
                await _db.SaveChangesAsync();

                // Tạo customer profile
                _db.Customers.Add(new Customer
                {
                    AccountId = account.AccountId,
                    FullName = dto.FullName
                });
                await _db.SaveChangesAsync();

                return Ok("Registered successfully");
            }
            catch(Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody]  LoginDto dto)
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
                    ExpiryDate = DateTime.UtcNow.AddDays(int.Parse(_config["JwtSettings:RefreshTokenExpirationDays"]))
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
                if (token == null || token.ExpiryDate < DateTime.UtcNow)
                    return Unauthorized("Invalid refresh token");

                var account = _db.Accounts.Include(a => a.Role).First(a => a.AccountId == token.AccountId);
                var newAccessToken = _tokenService.GenerateAccessToken(account);
                var newRefreshToken = _tokenService.GenerateRefreshToken();

                // Update token in DB
                token.RefreshToken = newRefreshToken;
                token.ExpiryDate = DateTime.UtcNow.AddDays(int.Parse(_config["JwtSettings:RefreshTokenExpirationDays"]));
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

                var subject = "Mật khẩu mới của bạn";
                var body = $@"
            <h2>Xin chào {user.Email},</h2>
            <p>Mật khẩu mới của bạn là: <b>{newPassword}</b></p>
            <p>Vui lòng đổi mật khẩu sau khi đăng nhập nhé!</p>
            <p>Trân trọng,<br/>Điện Máy Xanh</p>
        ";

                await _emailService.SendForgotPasswordEmail(dto.Email, newPassword);

                return Ok("A new password has been sent to your email.");
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
