using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ElectronicStore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet("getAll")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> GetAll(int pageNumber = 1, int pageSize = 10)
        {
            var result = await _orderService.GetAllOrdersAsync(pageNumber, pageSize);
            if (!result.Success)
                return StatusCode(500, result.Message);
            return Ok(result.Data);
        }
        [HttpGet("filter")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> FilterOrders(string status, int pageNumber = 1, int pageSize = 10)
        {
            var result = await _orderService.FilterOrdersAsync(status, pageNumber, pageSize);
            if (!result.Success)
                return StatusCode(500, result.Message);
            return Ok(result.Data);
        }


        [HttpGet("getByOrderCode/{orderCode}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> GetByOrderCode(string orderCode)
        {
            var result = await _orderService.GetOrderByOrderCodeAsync(orderCode);
            if (!result.Success)
                return NotFound(result.Message);
            return Ok(result.Data);
        }

        [HttpGet("getByCustomer")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetByCustomer()
        {
            var accountId = User.Claims.FirstOrDefault(c => c.Type == "AccountID")?.Value;
            if (accountId == null) return Unauthorized("Invalid token");

            var result = await _orderService.GetOrdersByCustomerAccountIdAsync(int.Parse(accountId));
            if (!result.Success)
                return NotFound(result.Message);
            return Ok(result.Data);
        }
        [HttpPut("update-status/{OrderCode}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> UpdateOrderStatus(string OrderCode, [FromBody] string newStatus)
        {
            var result = await _orderService.UpdateOrderStatusAsync(OrderCode, newStatus);
            if (!result.Success)
                return BadRequest(result.Message);
            return Ok(new { Message = result.Message, OrderCode = OrderCode, NewStatus = newStatus });
        }

        [HttpPost("CancelOrder")]
        [Authorize]
        public async Task<IActionResult> CancelOrder(string OrderCode)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            int? accountId = null;

            if (role == "Customer")
            {
                var accountIdStr = User.Claims.FirstOrDefault(c => c.Type == "AccountID")?.Value;
                accountId = int.Parse(accountIdStr);
            }

            var result = await _orderService.CancelOrderAsync(OrderCode, role, accountId);
            if (!result.Success)
                return result.Message.Contains("not found") ? NotFound(result.Message) : BadRequest(result.Message);
            return Ok(new { Message = result.Message, OrderCode });
        }
        [HttpPut("Refund")]
        public async Task<IActionResult> RefundOrder(string OrderCode)
        {
            var result = await _orderService.RefundOrderAsync(OrderCode);
            if (!result.Success)
                return result.Message.Contains("not found") ? NotFound(result.Message) : BadRequest(result.Message);
            return Ok(new { Message = result.Message, OrderCode });
        }
    }
}
