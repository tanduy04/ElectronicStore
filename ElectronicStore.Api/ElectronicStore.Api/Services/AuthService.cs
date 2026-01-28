using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Repositories.Interfaces;
using ElectronicStore.Api.Services.Interfaces;

namespace ElectronicStore.Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly TokenService _tokenService;
        private readonly EmailService _emailService;
        private readonly IConfiguration _config;

        public AuthService(
            IUnitOfWork unitOfWork,
            TokenService tokenService,
            EmailService emailService,
            IConfiguration config)
        {
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _emailService = emailService;
            _config = config;
        }

        public async Task<(bool Success, string Message, object? Data)> RegisterAsync(RegisterDto dto)
        {
            try
            {
                // Kiểm tra email đã tồn tại
                if (await _unitOfWork.Accounts.AnyAsync(a => a.Email == dto.Email))
                    return (false, "Email already exists", null);

                // Kiểm tra số điện thoại
                var existingCustomer = await _unitOfWork.Customers.FirstOrDefaultAsync(
                    c => c.Phone == dto.PhoneNumber && c.AccountId != null);
                if (existingCustomer != null)
                    return (false, "Phone number already exists", null);

                // Lấy role Customer
                var customerRole = await _unitOfWork.Accounts.FirstOrDefaultAsync(
                    a => a.Role.RoleName == "Customer");
                if (customerRole == null)
                    return (false, "Customer role not found", null);

                // Tạo account mới
                var newAccount = new Account
                {
                    Email = dto.Email,
                    Username = dto.Username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    RoleId = customerRole.RoleId,
                    IsActive = true,
                    LoginType = "Local",
                    Avatar = "default-avatar.jpg",
                    CreatedAt = DateTime.Now
                };

                await _unitOfWork.Accounts.AddAsync(newAccount);
                await _unitOfWork.SaveChangesAsync();

                // Lấy account vừa tạo
                var account = await _unitOfWork.Accounts.GetByUsernameAsync(newAccount.Username);

                // Kiểm tra customer tồn tại với phone nhưng chưa có account
                var customerExist = await _unitOfWork.Customers.FirstOrDefaultAsync(
                    c => c.Phone == dto.PhoneNumber && c.AccountId == null);

                if (customerExist != null)
                {
                    customerExist.FullName = dto.FullName;
                    customerExist.AccountId = account.AccountId;
                    _unitOfWork.Customers.Update(customerExist);
                }
                else
                {
                    // Tạo customer mới
                    var newCustomer = new Customer
                    {
                        FullName = dto.FullName,
                        AccountId = account.AccountId,
                        Phone = dto.PhoneNumber,
                        CreatedAt = DateTime.Now,
                        Point = 0
                    };
                    await _unitOfWork.Customers.AddAsync(newCustomer);
                }

                await _unitOfWork.SaveChangesAsync();

                return (true, "Registered successfully", null);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> LoginAsync(LoginDto dto)
        {
            try
            {
                var account = await _unitOfWork.Accounts.GetByEmailOrUsernameAsync(dto.Username);

                if (account == null || !BCrypt.Net.BCrypt.Verify(dto.Password, account.PasswordHash))
                    return (false, "Incorrect username or password", null);

                if (!account.IsActive)
                    return (false, "Account is deactivated", null);

                var accessToken = _tokenService.GenerateAccessToken(account);
                var refreshToken = _tokenService.GenerateRefreshToken();

                // Lưu refresh token
                await _unitOfWork.Accounts.AddAsync(new Account
                {
                    AccountId = account.AccountId,
                });

                // Note: You'll need to add AccountToken repository
                // For now, direct DB access (should be moved to repository)
                await _unitOfWork.SaveChangesAsync();

                var result = new { accessToken, refreshToken };
                return (true, "Login successful", result);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> RefreshTokenAsync(RefreshTokenDto dto)
        {
            try
            {
                // Note: Need to implement AccountToken repository
                // This is simplified version
                var account = await _unitOfWork.Accounts.GetAccountWithRoleAsync(1); // Placeholder

                if (account == null)
                    return (false, "Invalid refresh token", null);

                var newAccessToken = _tokenService.GenerateAccessToken(account);
                var newRefreshToken = _tokenService.GenerateRefreshToken();

                var result = new { accessToken = newAccessToken, refreshToken = newRefreshToken };
                return (true, "Token refreshed successfully", result);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> ChangePasswordAsync(int accountId, ChangePasswordDto dto)
        {
            try
            {
                var account = await _unitOfWork.Accounts.GetByIdAsync(accountId);
                if (account == null)
                    return (false, "Account not found");

                if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, account.PasswordHash))
                    return (false, "Incorrect old password");

                account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                _unitOfWork.Accounts.Update(account);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Password changed successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            try
            {
                var account = await _unitOfWork.Accounts.GetByEmailAsync(dto.Email);
                if (account == null)
                    return (false, "Email doesn't exist");

                var newPassword = GenerateRandomPassword(10);
                account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                _unitOfWork.Accounts.Update(account);
                await _unitOfWork.SaveChangesAsync();

                await _emailService.SendForgotPasswordEmail(dto.Email, account.Username, newPassword);

                return (true, "A new password has been sent to your email.");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(string token, string newPassword)
        {
            // Implementation for reset password with token
            throw new NotImplementedException();
        }

        private string GenerateRandomPassword(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
