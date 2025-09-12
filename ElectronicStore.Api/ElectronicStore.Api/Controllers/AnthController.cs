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
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (_db.Accounts.Any(a => a.Email == dto.Email))
                return BadRequest("Email already exists");

            var account = new Account
            {
                Email = dto.Email,
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RoleId = 3 // Customer
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

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var account = _db.Accounts.Include(a => a.Role).FirstOrDefault(a => a.Username == dto.Username);  
            if (account == null || !BCrypt.Net.BCrypt.Verify(dto.Password, account.PasswordHash))
                return Unauthorized("Invalid credentials");

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

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(RefreshTokenDto dto)
        {
            var token = _db.AccountTokens.FirstOrDefault(t => t.RefreshToken == dto.RefreshToken);
            if (token == null || token.ExpiryDate < DateTime.UtcNow)
                return Unauthorized("Invalid refresh token");

            var account = _db.Accounts.Include(a=> a.Role).First(a => a.AccountId == token.AccountId);
            var newAccessToken = _tokenService.GenerateAccessToken(account);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            // Update token in DB
            token.RefreshToken = newRefreshToken;
            token.ExpiryDate = DateTime.UtcNow.AddDays(int.Parse(_config["JwtSettings:RefreshTokenExpirationDays"]));
            await _db.SaveChangesAsync();

            return Ok(new { accessToken = newAccessToken, refreshToken = newRefreshToken });
        }
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto model)
        {
            // Lấy userId từ token
            var accountId = int.Parse(User.FindFirstValue("AccountID"));

            var account = await _db.Accounts.FirstOrDefaultAsync(x => x.AccountId == accountId);
            if (account == null)
                return NotFound("Không tìm thấy tài khoản");

            // Kiểm tra mật khẩu cũ
            if (!BCrypt.Net.BCrypt.Verify(model.OldPassword, account.PasswordHash))
                return BadRequest("Mật khẩu cũ không đúng");

            // Hash mật khẩu mới
            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            await _db.SaveChangesAsync();

            return Ok("Đổi mật khẩu thành công");
        }
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var user = await _db.Accounts.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
            {
                return BadRequest("Email không tồn tại trong hệ thống!");
            }

            // Tạo mật khẩu ngẫu nhiên
            var newPassword = "123456";
            //var newPassword = GenerateRandomPassword(10);

            // Hash mật khẩu
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _db.SaveChangesAsync();

            // Gửi email
            var subject = "Mật khẩu mới của bạn";
            var body = $@"
            <h2>Xin chào {user.Email},</h2>
            <p>Mật khẩu mới của bạn là: <b>{newPassword}</b></p>
            <p>Vui lòng đổi mật khẩu sau khi đăng nhập nhé!</p>
            <p>Trân trọng,<br/>Điện Máy Xanh</p>
        ";

            await _emailService.SendForgotPasswordEmail(dto.Email,newPassword);

            return Ok("Mật khẩu mới đã được gửi về email của bạn!");
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
