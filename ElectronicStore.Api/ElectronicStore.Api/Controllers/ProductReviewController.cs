using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductReviewController : ControllerBase
    {
        private readonly IProductReviewService _productReviewService;

        public ProductReviewController(IProductReviewService productReviewService)
        {
            _productReviewService = productReviewService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> GetAllReviews(bool isactive = false)
        {
            var result = await _productReviewService.GetAllReviewsAsync(isactive);

            if (!result.Success)
                return NotFound(result.Message);

            return Ok(result.Data);
        }
        [HttpDelete]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            var result = await _productReviewService.DeleteReviewAsync(reviewId);

            if (!result.Success)
                return NotFound(result.Message);

            return Ok(result.Message);
        }

        [HttpPost("replyReview")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> ReplyReview([FromForm] ReplyReview dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _productReviewService.ReplyToReviewAsync(dto);

            if (!result.Success)
                return result.Message.Contains("not found") ? NotFound(result.Message) : BadRequest(result.Message);

            return Ok(result.Message);
        }
        [HttpGet("ByProduct/{productId}")]
        public async Task<IActionResult> GetReviewsByProductId(int productId)
        {
            var result = await _productReviewService.GetReviewsByProductIdAsync(productId);

            if (!result.Success)
                return NotFound(result.Message);

            return Ok(result.Data);
        }
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromForm] NewProductReviewDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _productReviewService.CreateReviewAsync(dto);

            if (!result.Success)
                return result.Message.Contains("not found") ? NotFound(result.Message) : BadRequest(result.Message);

            return Ok(result.Message);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromForm] string content)
        {
            if (!ModelState.IsValid)
                return BadRequest();
            
            if (string.IsNullOrEmpty(content))
                return BadRequest();

            var accountId = User.FindFirst("AccountID")?.Value;
            if (accountId == null)
                return Unauthorized();

            var result = await _productReviewService.UpdateReviewAsync(id, content, int.Parse(accountId));

            if (!result.Success)
                return result.Message.Contains("not found") ? NotFound(result.Message) : Unauthorized(result.Message);

            return Ok(result.Message);
        }

    }

}
