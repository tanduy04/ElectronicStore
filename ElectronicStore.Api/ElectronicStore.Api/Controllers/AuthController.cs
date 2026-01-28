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

        public AuthController(IAuthService authService)
        {
            _authService = authService;
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
            var accountID = int.Parse(User.FindFirst("AccountID").Value);
            var role = User.IsInRole("Customer") ? "Customer" : "Employee";

            var result = await _authService.GetProfileAsync(accountID, role);
            
            if (!result.Success)
                return NotFound(result.Message);

            return Ok(result.Data);
        }
    }
}
