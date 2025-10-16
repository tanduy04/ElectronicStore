using ElectronicStore.Api.Service;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using ElectronicStore.Api.Helper; // Giả định SessionExtensions nằm ở đây
using ElectronicStore.Api.Dto; // Giả định ChatMessage nằm ở đây

namespace ElectronicStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatbotController : ControllerBase
    {
        private const string ChatHistoryKey = "ChatHistory"; // Khóa Session

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GeminiConfig _config;
        private readonly IVectorSearchService _vectorSearchService;

        public ChatbotController(IHttpClientFactory httpClientFactory, GeminiConfig config, IVectorSearchService vectorSearchService)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _vectorSearchService = vectorSearchService;
        }

        // ==========================================================
        // ***** PHƯƠNG THỨC MỚI: TẢI LỊCH SỬ CHAT (GET) *****
        // ==========================================================
        [HttpGet("history")]
        public ActionResult<List<ChatMessage>> GetChatHistory()
        {
            // Tải lịch sử chat từ Session
            // Sử dụng GetObjectFromJson<List<ChatMessage>> để đảm bảo type safety
            var history = HttpContext.Session.GetObjectFromJson<List<ChatMessage>>(ChatHistoryKey)
                          ?? new List<ChatMessage>();

            // Trả về danh sách tin nhắn. Nếu không có, trả về mảng rỗng (HTTP 200 OK)
            return Ok(history);
        }

        // ==========================================================
        // ***** PHƯƠNG THỨC GỬI TIN NHẮN (POST) - Giữ nguyên logic *****
        // ==========================================================
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest req)
        {
            // 1. TẢI LỊCH SỬ CHAT TỪ SESSION
            var history = HttpContext.Session.GetObjectFromJson<List<ChatMessage>>(ChatHistoryKey) ?? new List<ChatMessage>();

            // THÊM TIN NHẮN HIỆN TẠI CỦA NGƯỜI DÙNG VÀO LỊCH SỬ TRƯỚC KHI XỬ LÝ
            history.Add(new ChatMessage { Role = "user", Content = req.Message });

            // ----------------------------------------------------
            // ***** RAG - Tìm kiếm Dữ liệu liên quan *****
            // ----------------------------------------------------

            // 2. Chuyển câu hỏi người dùng thành vector nhúng
            float[] queryEmbedding = await _vectorSearchService.GetEmbeddingAsync(req.Message);

            // 3. Tìm kiếm 3 sản phẩm liên quan nhất
            var relevantProducts = await _vectorSearchService.SearchRelevantProductsAsync(queryEmbedding);

            // 4. Tạo ngữ cảnh (Context) từ dữ liệu sản phẩm
            var contextData = new StringBuilder();
            contextData.AppendLine("DỮ LIỆU SẢN PHẨM TỪ CSDL:");
            if (relevantProducts.Any())
            {
                foreach (var productChatbot in relevantProducts)
                {
                    contextData.AppendLine($"- Tên: {productChatbot.Name}, Giá: {productChatbot.Price:N0} VND, Mô tả: {productChatbot.Description}");
                }
            }
            else
            {
                contextData.AppendLine("Không tìm thấy sản phẩm liên quan. Trả lời dựa trên kiến thức chung.");
            }

            // ----------------------------------------------------
            // ***** TẠO PROMPT CUỐI CÙNG (Gộp Lịch sử và RAG) *****
            // ----------------------------------------------------

            // 5. Gộp Lịch sử Chat vào Prompt
            var chatHistoryContext = new StringBuilder();
            chatHistoryContext.AppendLine("LỊCH SỬ CUỘC TRÒ CHUYỆN:");
            // Chỉ gửi 10 tin nhắn gần nhất
            foreach (var message in history.TakeLast(10))
            {
                chatHistoryContext.AppendLine($"{message.Role}: {message.Content}");
            }

            // 6. Tạo PROMPT cuối cùng gửi đến Gemini
            var finalPrompt = $@"
                Bạn là trợ lý ảo của cửa hàng Điện máy xanh, hãy trả lời câu hỏi của người dùng một cách thân thiện, chính xác.
                Bạn PHẢI sử dụng thông tin được cung cấp trong phần DỮ LIỆU SẢN PHẨM để trả lời về sản phẩm, giá cả và mô tả nhưng đừng dùng từ cơ sở dữ liệu nhé mà hãy dùng cửa hàng của chúng tôi.
                Đối với các câu trả lời mục đích tìm kiếm thì chỉ lấy ra 3 cái thôi nhé.
                Đối với các câu hỏi liên quan đến bảo mật thì yêu cầu liên hệ hotline 1800 1061
                Nếu câu hỏi không liên quan đến sản phẩm, hãy trả lời bằng kiến thức chung.

                {contextData.ToString()}
                
                {chatHistoryContext.ToString()}

                CÂU HỎI HIỆN TẠI CỦA NGƯỜI DÙNG:
                {req.Message}";

            // -------------------------------------------------------------------
            // ***** GỌI API GEMINI VỚI PROMPT ĐÃ LÀM GIÀU *****
            // -------------------------------------------------------------------

            var client = _httpClientFactory.CreateClient("Gemini");
            var url = $"models/gemini-2.5-flash:generateContent?key={_config.ApiKey}";
            var body = new
            {
                contents = new[]
                {
                    new {
                        parts = new[] { new { text = finalPrompt } }
                    }
                }
            };

            var json = JsonSerializer.Serialize(body);
            var response = await client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Xóa tin nhắn người dùng khỏi lịch sử nếu gọi API lỗi
                history.Remove(history.Last());
                HttpContext.Session.SetObjectAsJson(ChatHistoryKey, history);
                return BadRequest(responseText);
            }

            // ----------------------------------------------------
            // ***** XỬ LÝ PHẢN HỒI GEMINI & LƯU LỊCH SỬ *****
            // ----------------------------------------------------
            try
            {
                var doc = JsonDocument.Parse(responseText);
                var finalAnswer = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text").GetString();

                // 7. LƯU LỊCH SỬ CHAT MỚI VÀO SESSION
                if (!string.IsNullOrEmpty(finalAnswer))
                {
                    // Thêm câu trả lời của AI vào lịch sử
                    history.Add(new ChatMessage { Role = "model", Content = finalAnswer! });

                    // Lưu lại toàn bộ List<ChatMessage> đã được cập nhật
                    HttpContext.Session.SetObjectAsJson(ChatHistoryKey, history);
                }

                return Ok(new { message = finalAnswer });
            }
            catch (Exception ex)
            {
                // Xóa tin nhắn người dùng khỏi lịch sử nếu phân tích cú pháp lỗi
                history.Remove(history.Last());
                HttpContext.Session.SetObjectAsJson(ChatHistoryKey, history);
                return StatusCode(500, new { error = "Lỗi phân tích cú pháp phản hồi từ Gemini.", details = ex.Message });
            }
        }
    }
    public class ChatRequest
    {
        public string Message { get; set; } = "";
    }
}