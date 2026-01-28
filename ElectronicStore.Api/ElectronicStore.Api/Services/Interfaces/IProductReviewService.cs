using ElectronicStore.Api.Dto;

namespace ElectronicStore.Api.Services.Interfaces
{
    public interface IProductReviewService
    {
        Task<(bool Success, string Message, object? Data)> GetAllReviewsAsync(bool isActive);
        Task<(bool Success, string Message, object? Data)> GetReviewsByProductIdAsync(int productId);
        Task<(bool Success, string Message, object? Data)> CreateReviewAsync(ProductReviewDto dto);
        Task<(bool Success, string Message, object? Data)> ReplyToReviewAsync(int reviewId, ProductReviewDto dto);
        Task<(bool Success, string Message)> UpdateReviewStatusAsync(int reviewId, bool isActive);
        Task<(bool Success, string Message)> DeleteReviewAsync(int reviewId);
    }
}
