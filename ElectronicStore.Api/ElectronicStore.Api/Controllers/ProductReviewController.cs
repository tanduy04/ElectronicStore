using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

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
        [HttpGet]
        [Authorize(Roles = "Admin,Employee")]

        public async Task<IActionResult> GetAllReviews(bool isactive = false)
        {
            try
            {
                var reviews = await _context.ProductReviews.Include(s => s.Product)
                .Where(r => r.ParentId == null && r.IsActive == isactive)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ProductReviewDto
                {
                    ReviewId = r.ReviewId,
                    ProductName = r.Product.ProductName,
                    FullName = r.FullName,
                    Phone = r.Phone,
                    Rating = r.Rating,
                    ParentId = r.ParentId,
                    Content = r.Content,
                    CreatedAt = r.CreatedAt,
                    IsActive = r.IsActive

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
                             Name = r.FullName,
                             Content = r.Content,
                             createAt = r.CreatedAt
                         })
               .FirstOrDefaultAsync();
                    if (childReview != null)
                        review.ReplyReview = childReview;
                }
                return Ok(reviews);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }

        }
        [HttpDelete]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            try
            {
                var review = await _context.ProductReviews.FirstOrDefaultAsync(r => r.ReviewId == reviewId);
                if (review == null)
                    return NotFound();
                _context.ProductReviews.Remove(review);
                await _context.SaveChangesAsync();
                return Ok("Delete review successfully.");
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
                replyReview.ProductId = reviewParent.ProductId;
                replyReview.FullName = "Nhân viên Điện máy xanh";
                replyReview.Phone = null;
                replyReview.ParentId = dto.ParentID;
                replyReview.Content = dto.Content;
                replyReview.Rating = reviewParent.Rating;
                replyReview.CreatedAt = DateTime.Now;
                replyReview.IsActive = true;
                _context.ProductReviews.Add(replyReview);
                reviewParent.IsActive = true;
                await _context.SaveChangesAsync();
                return Ok("Review created successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }
        [HttpGet("ByProduct/{productId}")]
        public async Task<IActionResult> GetReviewsByProductId(int productId)
        {
            var reviews = await _context.ProductReviews
                .Where(r => r.ProductId == productId && r.ParentId == null && r.IsActive == true)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ProductReviewDto
                {
                    ReviewId = r.ReviewId,
                    ProductName = r.Product.ProductName,
                    FullName = r.FullName,
                    Phone = r.Phone,
                    Rating = r.Rating,
                    ParentId = r.ParentId,
                    Content = r.Content,
                    CreatedAt = r.CreatedAt,
                    IsActive = r.IsActive

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
                         Name = r.FullName,
                         Content = r.Content,
                     })
           .FirstOrDefaultAsync();
                if (childReview != null)
                    review.ReplyReview = childReview;
            }
            return Ok(reviews);
        }
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromForm] NewProductReviewDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var productExist = await _context.Products.AnyAsync(p => p.ProductId == dto.ProductId);
                if (!productExist) return NotFound("Product not found.");
                ProductReview productReview = new ProductReview();
                productReview.ProductId = dto.ProductId;
                productReview.FullName = dto.FullName;
                productReview.Phone = dto.Phone;
                productReview.ParentId = null;
                productReview.Content = dto.Content;
                productReview.Rating = dto.Rating;
                productReview.CreatedAt = DateTime.Now;
                productReview.IsActive = false;
                _context.ProductReviews.Add(productReview);
                await _context.SaveChangesAsync();
                return Ok("Review created successfully.");
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
                int accountID = int.Parse(User.FindFirst("AccountID").Value);
                var review = await _context.ProductReviews.FirstOrDefaultAsync(r => r.ReviewId == id);
                if (review == null) return NotFound();
                //if (review.AccountId != accountID) return Unauthorized();
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
