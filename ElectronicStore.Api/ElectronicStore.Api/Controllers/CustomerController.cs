using Microsoft.AspNetCore.Mvc;
using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ElectronicStore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly ElectronicStoreContext _context;

        public CustomersController(ICustomerService customerService, ElectronicStoreContext context)
        {
            _customerService = customerService;
            _context = context;
        }
        [HttpGet]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> GetAll(int pageNumber = 1, int pageSize = 10)
        {
            var result = await _customerService.GetAllCustomersAsync(pageNumber, pageSize);
            if (!result.Success)
                return BadRequest(result.Message);
            return Ok(result.Data);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            int? requestingAccountId = null;
            string? role = null;

            if (User.IsInRole("Customer"))
            {
                requestingAccountId = int.Parse(User.FindFirst("AccountID").Value);
                role = "Customer";
            }

            var result = await _customerService.GetCustomerByIdAsync(id, requestingAccountId, role);
            if (!result.Success)
                return result.Message.Contains("not authorized") ? Forbid(result.Message) : NotFound(result.Message);
            return Ok(result.Data);
        }





        [HttpGet("search")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> SearchByPhone(string phone)
        {
            var result = await _customerService.SearchByPhoneAsync(phone);
            if (!result.Success)
                return result.Message.Contains("required") ? BadRequest(result.Message) : NotFound(result.Message);
            return Ok(result.Data);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Update(int id, [FromForm] CustomerDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _customerService.UpdateCustomerAsync(id, dto);
            if (!result.Success)
                return BadRequest(result.Message);
            return Ok(result.Message);
        }
        [HttpPut]
        [Route("EditMyProfile")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Update([FromForm] CustomerProfileDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var accountId = int.Parse(User.FindFirst("AccountID").Value);
            var result = await _customerService.UpdateProfileAsync(accountId, dto);

            if (!result.Success)
                return BadRequest(result.Message);
            return Ok(result.Message);
        }
    }
}
