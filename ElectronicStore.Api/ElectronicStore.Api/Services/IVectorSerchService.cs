using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;

namespace ElectronicStore.Api.Service
{
    // Đặt trong thư mục Services/Interfaces
    public interface IVectorSearchService
    {
        // Bước 1: Chuyển text thành vector nhúng bằng mô hình Gemini Embedding
        Task<float[]> GetEmbeddingAsync(string text);

        // Bước 2: Tìm kiếm các Product có liên quan nhất trong Vector DB/CSDL nội bộ
        // Trả về Product Entity để có đầy đủ thông tin tạo Context
        Task<List<ProductChatbot>> SearchRelevantProductsAsync(float[] queryEmbedding);
    }
}