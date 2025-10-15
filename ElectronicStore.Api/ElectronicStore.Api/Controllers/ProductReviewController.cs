using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class ProductReviewController : ControllerBase
    {
        private readonly ElectronicStoreContext _context;

        public ProductReviewController(ElectronicStoreContext context)
        {
            _context = context;
        }
        [HttpGet("ByProduct/{productId}")]
        public async Task<IActionResult> GetReviewsByProductId(int productId)
        {
            var reviews = await _context.ProductReviews
                .Where(r => r.ProductId == productId && r.ParentId == null)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ProductReviewDto
                {
                    ReviewId = r.ReviewId,
                    ProductId = r.ProductId,
                    AccountId = r.AccountId,
                    Name = r.Account.Email,
                    Rating = r.Rating,
                    ParentId = r.ParentId,
                    Content = r.Content,
                })
                .ToListAsync();
            if (reviews == null || reviews.Count == 0)
                return NotFound();
            foreach (var review in reviews)
            {
                var childReview = await _context.ProductReviews
                     .Where(cr => cr.ParentId == review.ReviewId)
                     .OrderByDescending(r => r.CreatedAt)
                     .Select(r => new ViewReplyReview
                     {
                         ParentID = r.ParentId.Value,
                         ReviewID = r.ReviewId,
                         Name = "Quản trị viên",
                         Content = r.Content,
                     })
           .FirstOrDefaultAsync();
                if (childReview != null)
                    review.ReplyReview = childReview;
            }
            return Ok(reviews);
        }
        [HttpPost("create")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create([FromForm] NewProductReviewDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                ProductReview productReview = new ProductReview();
                productReview.AccountId = int.Parse(User.FindFirst("AccountID").Value);
                productReview.ProductId = dto.ProductId;
                productReview.ParentId = null;
                productReview.Content = dto.Content;
                productReview.Rating = dto.Rating;
                productReview.CreatedAt = DateTime.UtcNow;
                productReview.IsActive = true;
                _context.ProductReviews.Add(productReview);
                await _context.SaveChangesAsync();
                return Ok("Review created successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }
        [HttpPost("replyReview")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> ReplyReview([FromForm] ReplyReview dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var reviewParent = await _context.ProductReviews.FirstOrDefaultAsync(r => r.ReviewId == dto.ParentID);
                if (reviewParent == null) return NotFound();
                ProductReview replyReview = new ProductReview();
                replyReview.AccountId = int.Parse(User.FindFirst("AccountID").Value); ;
                replyReview.ProductId = reviewParent.ProductId;
                replyReview.ParentId = dto.ParentID;
                replyReview.Content = dto.Content;
                replyReview.Rating = reviewParent.Rating;
                replyReview.CreatedAt = DateTime.UtcNow;
                _context.ProductReviews.Add(replyReview);
                await _context.SaveChangesAsync();
                return Ok("Reviểw created successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromForm] string content)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest();
                if (string.IsNullOrEmpty(content)) return BadRequest();
                int accountID= int.Parse(User.FindFirst("AccountID").Value);
                var review = await _context.ProductReviews.FirstOrDefaultAsync(r => r.ReviewId == id);
                if(review == null) return NotFound();
                if (review.AccountId != accountID) return Unauthorized();
                review.Content = content;
                 _context.Update(review);
                await _context.SaveChangesAsync();
                return Ok("Update Success!");
                
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

    }

}
