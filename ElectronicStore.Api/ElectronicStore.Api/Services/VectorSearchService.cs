using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Service;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text;
using System.Text.Json;

// Đặt trong thư mục Services
public class VectorSearchService : IVectorSearchService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GeminiConfig _config;
    private readonly ElectronicStoreContext _context;

    public VectorSearchService(ElectronicStoreContext context,IHttpClientFactory httpClientFactory, GeminiConfig config)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _context = context;
    }

    // A. PHƯƠNG THỨC 1: TẠO VECTOR NHÚNG (EMBEDDING)
    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        var client = _httpClientFactory.CreateClient("Gemini");

        // Dùng mô hình nhúng (ví dụ: embedding-001)
        var url = $"models/text-embedding-004:embedContent?key={_config.ApiKey}";
        // Lưu ý: Tên mô hình có thể khác tùy vào phiên bản mới nhất của Google

        var body = new
        {
            content = new { parts = new[] { new { text = text } } }
        };

        var json = JsonSerializer.Serialize(body);
        var response = await client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();

        var responseText = await response.Content.ReadAsStringAsync();

        // Trích xuất vector từ JSON response
        try
        {
            using (var doc = JsonDocument.Parse(responseText))
            {
                var embedding = doc.RootElement
                                   .GetProperty("embedding")
                                   .GetProperty("values")
                                   .EnumerateArray()
                                   .Select(v => (float)v.GetDouble())
                                   .ToArray();
                return embedding;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Lỗi phân tích cú pháp embedding response: {ex.Message}");
        }
    }

    

    public async Task<List<ProductChatbot>> SearchRelevantProductsAsync(float[] queryEmbedding)
    {
        // BƯỚC 1: Truy vấn tất cả Product từ CSDL và Ánh xạ (Projection)
        // Sẽ không cần JOIN hay WHERE ở đây vì bạn muốn lấy tất cả sản phẩm

        var allChatbotProducts = await _context.Products // Giả sử _context.Products là DbSet của Entity Product
            .OrderByDescending(p => p.ProductId) // Sắp xếp để lấy dữ liệu mới nhất (tùy chọn)
            .Select(p => new ProductChatbot
            {
                // Ánh xạ các trường từ Product Entity gốc (dùng tên thuộc tính của Product)
                Id = p.ProductId,
                Name = p.ProductName,
                Description = p.Description,
                Price = p.SellPrice, // Sử dụng SellPrice làm giá bán

                // Bỏ qua Vector Search: Gán Embedding là rỗng/null
                Embedding = Array.Empty<float>()
            })
            .AsNoTracking() // Tối ưu hóa đọc
            .ToListAsync();

        // BƯỚC 2: (Bỏ qua Vector Search và Cosine Similarity)

        // Trả về danh sách đã ánh xạ
        return allChatbotProducts;
    }
}