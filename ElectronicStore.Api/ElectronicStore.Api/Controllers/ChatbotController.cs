using ElectronicStore.Api.Service;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using ElectronicStore.Api.Helper; // Giả định SessionExtensions nằm ở đây
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Data;
using Microsoft.EntityFrameworkCore; // Giả định ChatMessage nằm ở đây

namespace ElectronicStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatbotController : ControllerBase
    {
        private const string ChatHistoryKey = "ChatHistory"; // Khóa Session

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GeminiConfig _Geminiconfig;
        private readonly ElectronicStoreContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public ChatbotController(IHttpClientFactory httpClientFactory, GeminiConfig GeminiConfig, IWebHostEnvironment env, IConfiguration config, ElectronicStoreContext context)
        {
            _httpClientFactory = httpClientFactory;
            _Geminiconfig = GeminiConfig;
            _context = context;
            _env = env;
            _config = config;
        }
        private string GetProductFolder()
        {
            return Path.Combine(_env.WebRootPath ?? string.Empty, _config["ImageSettings:ProductPath"] ?? string.Empty);
        }
        private string GetBaseUrl() => $"{Request.Scheme}://{Request.Host}/";
        // ==========================================================
        // ***** PHƯƠNG THỨC MỚI: TẢI LỊCH SỬ CHAT (GET) *****
        // ==========================================================
        [HttpGet("history")]
        public ActionResult<List<object>> GetChatHistory()
        {
            // Tải lịch sử chat từ Session
            var history = HttpContext.Session.GetObjectFromJson<List<ChatMessage>>(ChatHistoryKey)
                          ?? new List<ChatMessage>();

            // Trả về dạng hiển thị: với message đã được tách (nếu model trả về sản phẩm theo định dạng separator + JSON)
            var separator = "|||";
            var view = new List<object>();

            foreach (var item in history)
            {
                if (item == null)
                {
                    continue;
                }

                if (string.Equals(item.Role, "model", System.StringComparison.OrdinalIgnoreCase))
                {
                    var content = item.Content ?? string.Empty;
                    var messageText = content;
                    object? products = null;

                    var sepIndex = (content ?? string.Empty).LastIndexOf(separator);
                    if (sepIndex >= 0)
                    {
                        messageText = (content ?? string.Empty).Substring(0, sepIndex).Trim();
                        var rawProducts = (content ?? string.Empty).Substring(sepIndex + separator.Length).Trim();

                        var startArr = rawProducts.IndexOf('[');
                        var endArr = rawProducts.LastIndexOf(']');
                        string? productsPart = null;
                        if (startArr >= 0 && endArr > startArr)
                        {
                            productsPart = rawProducts.Substring(startArr, endArr - startArr + 1).Trim();
                        }
                        else
                        {
                            productsPart = rawProducts;
                        }

                        if (!string.IsNullOrEmpty(productsPart) && productsPart != "[]")
                        {
                            try
                            {
                                products = JsonDocument.Parse(productsPart).RootElement;
                            }
                            catch
                            {
                                products = null; // ignore parse errors for history view
                            }
                        }
                    }

                    if (products != null)
                    {
                        view.Add(new { role = item.Role, message = messageText, products, timestamp = item.Timestamp });
                    }
                    else
                    {
                        view.Add(new { role = item.Role, message = messageText, timestamp = item.Timestamp });
                    }
                }
                else
                {
                    // user or other roles: return simple shape
                    view.Add(new { role = item.Role, message = item.Content, timestamp = item.Timestamp });
                }
            }

            return Ok(view);
        }

        // ==========================================================
        // ***** PHƯƠNG THỨC GỬI TIN NHẮN (POST) - Giữ nguyên logic *****
        // ==========================================================
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest req)
        {
            var baseUrl = GetBaseUrl();
            // 1. TẢI LỊCH SỬ CHAT TỪ SESSION
            var history = HttpContext.Session.GetObjectFromJson<List<ChatMessage>>(ChatHistoryKey) ?? new List<ChatMessage>();

            // THÊM TIN NHẮN HIỆN TẠI CỦA NGƯỜI DÙNG VÀO LỊCH SỬ TRƯỚC KHI XỬ LÝ
            history.Add(new ChatMessage { Role = "user", Content = req.Message });

            // ----------------------------------------------------
            // ***** RAG - Tìm kiếm Dữ liệu liên quan *****
            // ----------------------------------------------------


            var relevantProductsRaw = await _context.Products
                .Select(p => new
                {
                    Name = p.ProductName,
                    Description = p.Description,
                    Price = p.SellPrice,
                    // Select only the raw image filename/relative url from the DB
                    MainImageUrl = p.ProductImages.Where(i => i.ImageMain).Select(i => i.UrlProductImage).FirstOrDefault()
                })
                .ToListAsync();

            // Build the full URL for images in-memory (avoid EF Core translation issues)
            var imagePath = _config["ImageSettings:ProductPath"] ?? string.Empty;
            var relevantProducts = relevantProductsRaw.Select(p => new
            {
                p.Name,
                p.Description,
                p.Price,
                ImageUrl = string.IsNullOrEmpty(p.MainImageUrl) ? null : $"{baseUrl}{imagePath}{p.MainImageUrl}"
            }).ToList();

            // 4. Tạo ngữ cảnh (Context) từ dữ liệu sản phẩm
            var contextData = new StringBuilder();
            contextData.AppendLine("DỮ LIỆU SẢN PHẨM TỪ CSDL:");
            if (relevantProducts.Any())
            {
                foreach (var productChatbot in relevantProducts)
                {
                    contextData.AppendLine($"- Tên: {productChatbot.Name}, Giá: {productChatbot.Price:N0} VND, Mô tả: {productChatbot.Description},imageUrl: {productChatbot.ImageUrl}");
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
            // YÊU CẦU ĐẦY ĐỦ VỀ ĐỊNH DẠNG TRẢ VỀ:
            // Để dễ phân tách và hiển thị trên client, yêu cầu model trả lời ngắn gọn theo định dạng sau:
            // 1) PHẦN MESSAGE (chuỗi tự nhiên) - đây là nội dung người dùng sẽ thấy
            // 2) Một dòng chứa chính xác ký tự phân tách: |||
            // 3) PHẦN PRODUCTS: một JSON array chứa các đối tượng sản phẩm với trường name, price, description
            //    Nếu không có sản phẩm liên quan, trả về một mảng rỗng: []
            // Ví dụ trả về (khoảng cách/format phải giống):
            //    Cửa hàng hiện có 3 sản phẩm phù hợp...\n|||\n[{"name":"A","price":1000,"description":"..."}, ...]

            var finalPrompt = @"
                     Bạn là trợ lý ảo của cửa hàng Điện máy xanh. Trả lời ngắn gọn, thân thiện và chính xác.
                     HÃY CHÚ Ý RẤT QUAN TRỌNG ĐẾN ĐỊNH DẠNG:
                     1) ĐẦU TIÊN in PHẦN MESSAGE (một đoạn văn bằng tiếng Việt, không chứa JSON hay ký tự phân tách).
                     2) In một dòng chỉ chứa chính xác ký tự phân tách: ||| (ba ký tự | liên tiếp) với không có ký tự khác trên dòng đó.
                     3) NGAY SAU DÒNG PHÂN TÁCH, in CHÍNH XÁC một JSON ARRAY (ví dụ: [{""name"":""..."",""price"":12345,""description"":""..."",""imageUrl"":""...""}, ...]).
                         - Mỗi object trong array phải có khóa chính xác: ""name"", ""price"", ""description"",""imageUrl"".
                         - ""price"" phải là một số (integer) biểu diễn VND (ví dụ: 2390000).
                         - KHÔNG in thêm chú thích, ký hiệu tiền tệ, hay văn bản nào khác cùng với JSON.
                     4) Nếu KHÔNG có sản phẩm liên quan, phần PRODUCTS phải là [] (một mảng JSON rỗng).
                        trả về tối đa 3 sản phẩm thôi nhé
                     Nếu bạn không trả lời về sản phẩm thì vẫn phải tuân thủ định dạng: phần PRODUCTS = [] sau dòng phân tách.

                     SỬ DỤNG THÔNG TIN DỮ LIỆU SẢN PHẨM dưới đây để tạo ra output (nhưng KHÔNG nói 'cơ sở dữ liệu'):
                     ";

            // Nối phần dữ liệu động vào prompt (tránh dùng interpolated string để không cần escape braces)
            finalPrompt += "\n" + "DỮ LIỆU SẢN PHẨM :\n" + contextData.ToString() + "\n\n";
            finalPrompt += "LỊCH SỬ CUỘC TRÒ CHUYỆN :\n" + chatHistoryContext.ToString() + "\n\n";
            finalPrompt += "CÂU HỎI HIỆN TẠI CỦA NGƯỜI DÙNG:\n" + req.Message + "\n";

            // -------------------------------------------------------------------
            // ***** GỌI API GEMINI VỚI PROMPT ĐÃ LÀM GIÀU *****
            // -------------------------------------------------------------------

            var client = _httpClientFactory.CreateClient("Gemini");
            var url = $"models/gemini-2.5-flash:generateContent?key={_Geminiconfig.ApiKey}";
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

                // 7. LƯU LỊCH SỬ CHAT MỚI VÀO SESSION + PHÂN TÍCH ĐỊNH DẠNG NGẮN
                if (!string.IsNullOrEmpty(finalAnswer))
                {
                    // Lưu toàn bộ output của model vào lịch sử (giữ nguyên để truy vết)
                    history.Add(new ChatMessage { Role = "model", Content = finalAnswer! });
                    HttpContext.Session.SetObjectAsJson(ChatHistoryKey, history);

                    // Tách theo separator đã yêu cầu trong prompt
                    var separator = "|||";
                    string messageText = finalAnswer;
                    string? productsPart = null;

                    // Sử dụng last index để tránh trường hợp message chứa separator
                    var sepIndex = finalAnswer.LastIndexOf(separator);
                    if (sepIndex >= 0)
                    {
                        messageText = finalAnswer.Substring(0, sepIndex).Trim();
                        var rawProducts = finalAnswer.Substring(sepIndex + separator.Length).Trim();

                        // Nếu model đưa kèm một vài text trước/sau JSON, cố gắng tách lấy phần JSON array bằng cách tìm '[' và ']'
                        var startArr = rawProducts.IndexOf('[');
                        var endArr = rawProducts.LastIndexOf(']');
                        if (startArr >= 0 && endArr > startArr)
                        {
                            productsPart = rawProducts.Substring(startArr, endArr - startArr + 1).Trim();
                        }
                        else
                        {
                            // fallback: dùng toàn bộ phần còn lại
                            productsPart = rawProducts;
                        }
                    }

                    // Nếu có phần products và khác [] thì cố gắng parse JSON và trả về kèm products
                    if (!string.IsNullOrEmpty(productsPart) && productsPart != "[]")
                    {
                        try
                        {
                            var productsJson = JsonDocument.Parse(productsPart).RootElement;
                            return Ok(new { message = messageText, products = productsJson });
                        }
                        catch
                        {
                            // Nếu parse thất bại, fallback trả về message duy nhất
                            return Ok(new { message = messageText });
                        }
                    }
                    else
                    {
                        return Ok(new { message = messageText });
                    }
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