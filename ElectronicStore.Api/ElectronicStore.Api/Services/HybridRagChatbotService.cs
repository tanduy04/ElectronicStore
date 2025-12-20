using ElectronicStore.Api.Data;
using ElectronicStore.Api.Models;
using ElectronicStore.Api.Services;
using Microsoft.EntityFrameworkCore;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.Buffers.Text;
using System.Text;
using System.Text.Json;

namespace ElectronicStore.Api.Services
{

    // Service chính cho Hybrid Chatbot
    public class HybridRagChatbotService
    {
        private readonly GeminiService _geminiService;
        private readonly QdrantService _qdrantService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HybridRagChatbotService> _logger;
        private readonly ElectronicStoreContext _dbContext;

        // Giả định có DB context - thay bằng context thực của bạn
        // private readonly YourDbContext _dbContext;

        public HybridRagChatbotService(
            GeminiService geminiService,
            QdrantService qdrantService,
            IConfiguration configuration,
            ILogger<HybridRagChatbotService> logger,
            ElectronicStoreContext dbContext) // THÊM DbContext
        {
            _geminiService = geminiService;
            _qdrantService = qdrantService;
            _configuration = configuration;
            _logger = logger;
            _dbContext = dbContext; // THÊM
        }
        private string GetBaseUrl() => _configuration["AppSettings:BaseUrl"];
        public async Task<HybridChatResponse> ProcessQuestionAsync(
            string question,
            List<Brand> brands,
            List<Category> categories)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // STEP 1: Phân tích ý định người dùng bằng Gemini
                _logger.LogInformation("Step 1: Analyzing user intent...");
                var filter = await AnalyzeUserIntentAsync(question, brands, categories);

                if (filter == null)
                {
                    return new HybridChatResponse
                    {
                        message = "Xin lỗi, tôi không thể phân tích câu hỏi của bạn.",
                        ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                    };
                }

                // STEP 2A: Nếu KHÔNG phải hỏi về sản phẩm -> Dùng RAG
                if (!filter.IsProductQuery)
                {
                    _logger.LogInformation("Step 2A: Using RAG for general question...");

                    // Tìm context từ Qdrant
                    var contexts = await _qdrantService.SearchSimilarAsync(question, topK: 3);

                    // Generate answer từ Gemini với context
                    var answer = await _geminiService.GenerateAnswerAsync(question, contexts);

                    stopwatch.Stop();
                    return new HybridChatResponse
                    {
                        message = answer ?? "Xin lỗi, tôi không thể tạo câu trả lời lúc này.",
                        RetrievedContexts = contexts,
                        IsProductQuery = false,
                        ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                    };
                }

                // STEP 2B: Nếu hỏi về sản phẩm -> Query database
                _logger.LogInformation("Step 2B: Querying products from database...");
                var products = await QueryProductsAsync(filter);

                // STEP 3: Generate response message với danh sách sản phẩm
                _logger.LogInformation("Step 3: Generating product response...");
                var productAnswer = await GenerateProductResponseAsync(question, products);

                stopwatch.Stop();
                return new HybridChatResponse
                {
                    message = productAnswer,
                    Products = products,
                    IsProductQuery = true,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessQuestionAsync");
                stopwatch.Stop();
                return new HybridChatResponse
                {
                    message = "Xin lỗi, đã có lỗi xảy ra khi xử lý câu hỏi của bạn.",
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
        }

        // STEP 1: Phân tích ý định
        private async Task<ProductFilterDto?> AnalyzeUserIntentAsync(
            string question,
            List<Brand> brands,
            List<Category> categories)
        {
            try
            {
                // Tạo context từ brands và categories
                var contextData = new StringBuilder();
                contextData.AppendLine("DANH SÁCH THƯƠNG HIỆU (BRANDS):");
                foreach (var b in brands)
                {
                    contextData.AppendLine($"- ID: {b.BrandId}, Tên: {b.BrandName}");
                }

                contextData.AppendLine("\nDANH SÁCH DANH MỤC (CATEGORIES):");
                foreach (var c in categories)
                {
                    contextData.AppendLine($"- ID: {c.CategoryId}, Tên: {c.CategoryName}");
                }

                var intentPrompt = $@"
Bạn là trợ lý ảo của cửa hàng điện tử. 

NHIỆM VỤ: Phân tích câu hỏi của người dùng và TRẢ VỀ CHỈ MỘT JSON hợp lệ theo schema sau:

{{
  ""isProductQuery"": true/false,
  ""keywords"": [/* các từ khóa tìm kiếm */],
  ""minPrice"": /* số hoặc null */,
  ""maxPrice"": /* số hoặc null */,
  ""cheapest"": true/false,
  ""mostExpensive"": true/false,
  ""categoryIds"": [/* mảng ID danh mục */],
  ""brandIds"": [/* mảng ID thương hiệu */],
  ""limit"": /* số lượng sản phẩm 1-10 */,
  ""message"": ""/* nếu isProductQuery=false, câu trả lời ngắn */""
}}

HƯỚNG DẪN:
- Nếu người dùng KHÔNG hỏi về sản phẩm (chào hỏi, hỏi thông tin shop, chính sách) → isProductQuery=false
- Nếu người dùng HỎI VỀ SẢN PHẨM (tìm điện thoại, laptop, giá bao nhiêu...) → isProductQuery=true
- Sử dụng danh sách BRANDS và CATEGORIES để map tên thành ID
- KHÔNG in thêm text, CHỈ JSON

{contextData}

CÂU HỎI: {question}
";

                var response = await _geminiService.CallGeminiRawAsync(intentPrompt);
                if (string.IsNullOrEmpty(response))
                {
                    return null;
                }

                // Parse JSON từ response
                var start = response.IndexOf('{');
                var end = response.LastIndexOf('}');
                if (start < 0 || end <= start)
                {
                    _logger.LogWarning("Cannot find JSON in Gemini response");
                    return null;
                }

                var jsonOnly = response.Substring(start, end - start + 1);
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var filter = JsonSerializer.Deserialize<ProductFilterDto>(jsonOnly, opts);

                return filter;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AnalyzeUserIntentAsync");
                return null;
            }
        }

        // STEP 2B: Query products từ database (mock - thay bằng DB thực)
        private async Task<List<ProductInfo>> QueryProductsAsync(ProductFilterDto filter)
        {
            var imagePath = _configuration["ImageSettings:ProductPath"] ?? string.Empty;
            var baseUrl = GetBaseUrl();

            var query = _dbContext.Products.Include(p => p.ProductImages).AsQueryable();

            // Apply filters
            query = query.Where(p => p.IsActive);

            if (filter.MinPrice.HasValue)
                query = query.Where(p => p.SellPrice >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                query = query.Where(p => p.SellPrice <= filter.MaxPrice.Value);

            if (filter.CategoryIds?.Any() == true)
                query = query.Where(p => filter.CategoryIds.Contains(p.CategoryId));

            if (filter.BrandIds?.Any() == true)
                query = query.Where(p => filter.BrandIds.Contains(p.BrandId));

            if (filter.Keywords?.Any() == true)
            {
                foreach (var kw in filter.Keywords)
                {
                    var k = kw?.Trim();
                    if (!string.IsNullOrEmpty(k))
                        query = query.Where(p => EF.Functions.Like(p.ProductName, $"%{k}%"));
                }
            }

            // Sorting
            if (filter.Cheapest)
                query = query.OrderBy(p => p.SellPrice);
            else if (filter.MostExpensive)
                query = query.OrderByDescending(p => p.SellPrice);

            var limit = filter.Limit ?? 3;

            //var products = await query
            //    .Select(p => new ProductInfo
            //    {
            //        Id = p.ProductId,
            //        Name = p.ProductName,
            //        Price = p.SellPrice,
            //        ImageUrl = string.IsNullOrEmpty(p.ProductImages.) ? null : $"{baseUrl}{imagePath}{r.MainImage}"
            //    })
            //    .Take(limit)
            //    .ToListAsync();

            var products = await query.Select(p => new
            {
                p.ProductId,
                p.ProductName,
                p.Description,
                p.SellPrice,
                MainImage = p.ProductImages.Where(i => i.ImageMain).Select(i => i.UrlProductImage).FirstOrDefault()
            }).Take(limit).ToListAsync();

            var productsForClient = products.Select(r =>
            {
                // Parse description từ JSON sang text


                return new ProductInfo
                {
                    Id = r.ProductId,
                    Name = r.ProductName,
                    Price = r.SellPrice,
                    ImageUrl = string.IsNullOrEmpty(r.MainImage) ? null : $"{baseUrl}{imagePath}{r.MainImage}"
                };
            }).ToList();
            return productsForClient;
        }

        // STEP 3: Generate response message cho sản phẩm
        private async Task<string> GenerateProductResponseAsync(
            string question,
            List<ProductInfo> products)
        {
            if (!products.Any())
            {
                return "Xin lỗi, tôi không tìm thấy sản phẩm phù hợp với yêu cầu của bạn.";
            }

            var productsJson = JsonSerializer.Serialize(products.Select(p => new
            {
                p.Name,
                Price = $"{p.Price:N0} VNĐ"
            }));

            var prompt = $@"
Bạn là trợ lý bán hàng thân thiện.

Câu hỏi khách hàng: {question}

Danh sách sản phẩm tìm được:
{productsJson}

YÊU CẦU: Viết 1-2 câu giới thiệu ngắn gọn, thân thiện bằng tiếng Việt.
VÍ DỤ: 'Mình tìm thấy {products.Count} sản phẩm phù hợp cho bạn. Đây là những lựa chọn tốt nhất!'

CHỈ TRẢ TEXT, KHÔNG JSON.
";

            var response = await _geminiService.CallGeminiRawAsync(prompt);

            return string.IsNullOrEmpty(response)
                ? $"Mình tìm thấy {products.Count} sản phẩm phù hợp cho bạn."
                : response;
        }
    }

    // Extension method cho GeminiService để gọi raw
    public static class GeminiServiceExtensions
    {
        public static async Task<string?> CallGeminiRawAsync(
            this GeminiService service,
            string prompt)
        {
            // Sử dụng reflection hoặc refactor GeminiService để expose method này
            // Tạm thời return mock
            await Task.Delay(100);
            return "Mock response from Gemini";
        }
    }

    // DTO classes
    public class BrandDto
    {
        public int BrandId { get; set; }
        public string BrandName { get; set; } = string.Empty;
    }

    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }
}