using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Repositories.Interfaces;
using ElectronicStore.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStore.Api.Services
{
    public class ProductReviewService : IProductReviewService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ElectronicStoreContext _context;

        public ProductReviewService(
            IUnitOfWork unitOfWork,
            ElectronicStoreContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }

        public async Task<(bool Success, string Message, object? Data)> GetAllReviewsAsync(bool isActive)
        {
            try
            {
                var reviews = await _context.ProductReviews
                    .Include(r => r.Product)
                    .Where(r => r.ParentId == null && r.IsActive == isActive)
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

                if (!reviews.Any())
                    return (false, "No reviews found", null);

                // Get replies for each review
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

                return (true, "Success", reviews);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> GetReviewsByProductIdAsync(int productId)
        {
            try
            {
                var reviews = await _unitOfWork.ProductReviews.FindAsync(
                    r => r.ProductId == productId && r.IsActive == true);

                return (true, "Success", reviews);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> CreateReviewAsync(ProductReviewDto dto)
        {
            try
            {
                var review = new ProductReview
                {
                    ProductId = dto.ProductId,
                    FullName = dto.FullName,
                    Phone = dto.Phone,
                    Rating = dto.Rating,
                    Content = dto.Content,
                    CreatedAt = DateTime.Now,
                    IsActive = false, // Needs approval
                    ParentId = null
                };

                await _unitOfWork.ProductReviews.AddAsync(review);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Review submitted successfully", review);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> ReplyToReviewAsync(int reviewId, ProductReviewDto dto)
        {
            try
            {
                var parentReview = await _unitOfWork.ProductReviews.GetByIdAsync(reviewId);
                if (parentReview == null)
                    return (false, "Parent review not found", null);

                var reply = new ProductReview
                {
                    ProductId = parentReview.ProductId,
                    FullName = dto.FullName,
                    Phone = dto.Phone,
                    Rating = 0, // Reply doesn't have rating
                    Content = dto.Content,
                    CreatedAt = DateTime.Now,
                    IsActive = true,
                    ParentId = reviewId
                };

                await _unitOfWork.ProductReviews.AddAsync(reply);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Reply added successfully", reply);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> UpdateReviewStatusAsync(int reviewId, bool isActive)
        {
            try
            {
                var review = await _unitOfWork.ProductReviews.GetByIdAsync(reviewId);
                if (review == null)
                    return (false, "Review not found");

                review.IsActive = isActive;
                _unitOfWork.ProductReviews.Update(review);
                await _unitOfWork.SaveChangesAsync();

                return (true, $"Review {(isActive ? "approved" : "rejected")} successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteReviewAsync(int reviewId)
        {
            try
            {
                var review = await _unitOfWork.ProductReviews.GetByIdAsync(reviewId);
                if (review == null)
                    return (false, "Review not found");

                // Delete replies first
                var replies = await _unitOfWork.ProductReviews.FindAsync(r => r.ParentId == reviewId);
                if (replies.Any())
                {
                    _unitOfWork.ProductReviews.RemoveRange(replies);
                }

                _unitOfWork.ProductReviews.Remove(review);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Review deleted successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }
    }
}
