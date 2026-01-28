using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Helper;
using ElectronicStore.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CheckoutController : ControllerBase
    {
        private readonly ICheckoutService _checkoutService;

        public CheckoutController(ICheckoutService checkoutService)
        {
            _checkoutService = checkoutService;
        }
        [Authorize]
        [HttpGet("check-voucher/{voucherCode}")]
        public async Task<IActionResult> CheckVoucher(string voucherCode)
        {
            var accountId = User.FindFirst("AccountID")?.Value;
            if (accountId == null) return Unauthorized();

            var result = await _checkoutService.CheckVoucherAsync(voucherCode, int.Parse(accountId));
            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }
        [HttpPost("cod")]
        [Authorize]
        public async Task<IActionResult> CheckoutCOD(CheckoutCartDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var accountId = User.FindFirst("AccountID")?.Value;
            if (accountId == null) return Unauthorized();

            var result = await _checkoutService.CheckoutCODAsync(dto, int.Parse(accountId));
            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }
        [HttpPost("CreateVnPayPayment")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateVnPayPayment(CheckoutCartDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var accountId = User.Claims.FirstOrDefault(c => c.Type == "AccountID")?.Value;
            if (accountId == null) return Unauthorized("Invalid token.");

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var result = await _checkoutService.CheckoutVNPayAsync(dto, int.Parse(accountId), ipAddress);
            
            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }

        [HttpPost("Payment-without-login")]
        public async Task<IActionResult> ByNow(CheckoutProductDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var result = await _checkoutService.CheckoutWithoutLoginAsync(dto, ipAddress);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }

        [HttpGet("VnPayReturn")]
        public async Task<IActionResult> VnPayReturn([FromQuery] Dictionary<string, string> vnpParams)
        {
            var result = await _checkoutService.ProcessVNPayCallbackAsync(vnpParams);
            
            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }

    }
}
