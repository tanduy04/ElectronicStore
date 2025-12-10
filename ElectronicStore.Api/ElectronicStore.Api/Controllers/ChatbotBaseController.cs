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
    public class ChatbotBaseController : ControllerBase
    {
        private const string ChatHistoryKey = "ChatHistory"; // Khóa Session

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GeminiConfig _Geminiconfig;
        private readonly ElectronicStoreContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public ChatbotBaseController(IHttpClientFactory httpClientFactory, GeminiConfig GeminiConfig, IWebHostEnvironment env, IConfiguration config, ElectronicStoreContext context)
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
        private string GetBaseUrl() => _config["AppSettings:BaseUrl"];
        // ==========================================================
        // ***** PHƯƠNG THỨC MỚI: TẢI LỊCH SỬ CHAT (GET) *****
        // ==========================================================
        [HttpGet("historyy")]
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
        // ***** PHƯƠNG THỨC GỬI TIN NHẮN (POST) - 2-step Gemini calls *****
        // ==========================================================
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest1 req)
        {
            var baseUrl = GetBaseUrl();
            // 1. TẢI LỊCH SỬ CHAT TỪ SESSION
            var history = HttpContext.Session.GetObjectFromJson<List<ChatMessage>>(ChatHistoryKey) ?? new List<ChatMessage>();

            // THÊM TIN NHẮN HIỆN TẠI CỦA NGƯỜI DÙNG VÀO LỊCH SỬ TRƯỚC KHI XỬ LÝ
            history.Add(new ChatMessage { Role = "user", Content = req.Message });

            // ----------------------------------------------------
            // ***** STEP 1: Load Brands & Categories as context (không load products) *****
            // ----------------------------------------------------
            var brands = await _context.Brands
                .Where(b => b.IsActive)
                .Select(b => new { b.BrandId, b.BrandName })
                .ToListAsync();

            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .Select(c => new { c.CategoryId, c.CategoryName })
                .ToListAsync();
            var questionAndAnswer = await _context.QuestionAndAnswers
                .Select(q => new { q.Question, q.Answer })
                .ToListAsync();

            // 4. Tạo ngữ cảnh (Context) từ brands và categories
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
            contextData.AppendLine("\nDanh sach Q&A ():");
            foreach (var q in questionAndAnswer)
            {
                contextData.AppendLine($"- Câu hỏi: {q.Question}, Câu trả lời: {q.Answer}");
            }

            // ----------------------------------------------------
            // ***** TẠO PROMPT CUỐI CÙNG - STEP 1: ANALYZE INTENT *****
            // ----------------------------------------------------

            // 5. Gộp Lịch sử Chat vào Prompt
            var chatHistoryContext = new StringBuilder();
            chatHistoryContext.AppendLine("LỊCH SỬ CUỘC TRÒ CHUYỆN:");
            // Chỉ gửi 10 tin nhắn gần nhất
            foreach (var message in history.TakeLast(10))
            {
                chatHistoryContext.AppendLine($"{message.Role}: {message.Content}");
            }

            // 6. Tạo PROMPT cho GEMINI LẦN 1: Phân tích ý định và sinh filter
            var intentPrompt = @"
Bạn là trợ lý ảo của cửa hàng Điện máy xanh. 

NHIỆM VỤ: Phân tích câu hỏi của người dùng và TRẢ VỀ CHỈ MỘT JSON hợp lệ theo schema sau:

{
  ""isProductQuery"": true/false,
  ""keywords"": [/* các từ khóa tìm kiếm */],
  ""minPrice"": /* số hoặc null */,
  ""maxPrice"": /* số hoặc null */,
  ""Cheapest"": true/false,
  ""MostExpensive"": true/false,
  ""categoryIds"": [/* mảng ID danh mục */],
  ""brandIds"": [/* mảng ID thương hiệu */],
  ""limit"": /* số lượng sản phẩm tối đa 1-10 */,
  ""message"": ""/* nếu isProductQuery=false, đây là câu trả lời trực tiếp cho user (nếu liên đến bảo mật thì kêu liên hệ 1900 1068 để gặp trực tiếp nhân viên hỗ trợ) */""
}

HƯỚNG DẪN:
- Nếu người dùng KHÔNG hỏi về sản phẩm (ví dụ: chào hỏi, hỏi thông tin cửa hàng, v.v.) → set isProductQuery=false và viết câu trả lời vào trường ""message""
- Nếu người dùng HỎI VỀ SẢN PHẨM → set isProductQuery=true và điền các trường filter phù hợp
- Sử dụng danh sách BRANDS và CATEGORIES dưới đây để map tên thành ID
- KHÔNG in thêm text nào khác ngoài JSON object

";
            intentPrompt += "\n" + contextData.ToString() + "\n\n";
            intentPrompt += chatHistoryContext.ToString() + "\n\n";
            intentPrompt += "CÂU HỎI HIỆN TẠI:\n" + req.Message + "\n";

            // -------------------------------------------------------------------
            // ***** GỌI API GEMINI LẦN 1: PHÂN TÍCH Ý ĐỊNH *****
            // -------------------------------------------------------------------

            var client = _httpClientFactory.CreateClient("Gemini");
            var url = $"models/gemini-2.5-flash-lite:generateContent?key={_Geminiconfig.ApiKey}";
            var body = new
            {
                contents = new[]
                {
                    new {
                        parts = new[] { new { text = intentPrompt } }
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
            // ***** XỬ LÝ PHẢN HỒI GEMINI LẦN 1 - PARSE INTENT & FILTER *****
            // ----------------------------------------------------
            try
            {
                var doc = JsonDocument.Parse(responseText);
                var intentResponse = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text").GetString() ?? string.Empty;

                // Parse JSON từ response
                var start = intentResponse.IndexOf('{');
                var end = intentResponse.LastIndexOf('}');
                if (start < 0 || end <= start)
                {
                    // Fallback nếu không parse được
                    history.Remove(history.Last());
                    HttpContext.Session.SetObjectAsJson(ChatHistoryKey, history);
                    return StatusCode(500, new { error = "Không thể phân tích phản hồi từ Gemini." });
                }

                var jsonOnly = intentResponse.Substring(start, end - start + 1);
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var filter = JsonSerializer.Deserialize<ProductFilterDto>(jsonOnly, opts);

                if (filter == null)
                {
                    history.Remove(history.Last());
                    HttpContext.Session.SetObjectAsJson(ChatHistoryKey, history);
                    return StatusCode(500, new { error = "Không thể parse filter từ Gemini." });
                }

                // ----------------------------------------------------
                // ***** CASE 1: Không phải câu hỏi về sản phẩm *****
                // ----------------------------------------------------
                if (!filter.IsProductQuery)
                {
                    var simpleMessage = filter.Message ?? "Xin chào! Tôi có thể giúp gì cho bạn?";
                    history.Add(new ChatMessage { Role = "model", Content = simpleMessage, Timestamp = DateTime.Now });
                    HttpContext.Session.SetObjectAsJson(ChatHistoryKey, history);
                    return Ok(new { message = simpleMessage });
                }

                // ----------------------------------------------------
                // ***** CASE 2: Câu hỏi về sản phẩm - QUERY DATABASE *****
                // ----------------------------------------------------
                var limit = 3;
                var imagePath = _config["ImageSettings:ProductPath"] ?? string.Empty;

                var q = _context.Products.AsQueryable();
                q = q.Where(p => p.IsActive);

                if (filter.MinPrice.HasValue)
                    q = q.Where(p => p.SellPrice >= filter.MinPrice.Value);
                if (filter.MaxPrice.HasValue)
                    q = q.Where(p => p.SellPrice <= filter.MaxPrice.Value);
                if (filter.CategoryIds?.Any() == true)
                    q = q.Where(p => filter.CategoryIds.Contains(p.CategoryId));
                if (filter.BrandIds?.Any() == true)
                    q = q.Where(p => filter.BrandIds.Contains(p.BrandId));
                if (filter.Cheapest)
                {
                    q = q.OrderBy(p => p.SellPrice);
                }
                else if (filter.MostExpensive)
                {
                    q = q.OrderByDescending(p => p.SellPrice);
                }
                if (filter.Keywords?.Any() == true)
                {
                    foreach (var kw in filter.Keywords)
                    {
                        var k = kw?.Trim();
                        if (string.IsNullOrEmpty(k)) continue;
                        q = q.Where(p => EF.Functions.Like(p.ProductName, $"%{k}%"));
                    }
                }
                if (filter.Cheapest == true || filter.MostExpensive == true)
                    limit = 1;
                var rows = await q.Select(p => new
                {
                    p.ProductId,
                    p.ProductName,
                    p.Description,
                    p.SellPrice,
                    MainImage = p.ProductImages.Where(i => i.ImageMain).Select(i => i.UrlProductImage).FirstOrDefault()
                }).Take(limit).ToListAsync();

                var productsForClient = rows.Select(r =>
                {
                    // Parse description từ JSON sang text


                    return new
                    {
                        id= r.ProductId,
                        name = r.ProductName,
                        price = r.SellPrice,
                        imageUrl = string.IsNullOrEmpty(r.MainImage) ? null : $"{baseUrl}{imagePath}{r.MainImage}"
                    };
                }).ToList();

                // ----------------------------------------------------
                // ***** GỌI GEMINI LẦN 2: TẠO RESPONSE MESSAGE *****
                // ----------------------------------------------------
                var productsJson = JsonSerializer.Serialize(productsForClient);
                var shortHistory = string.Join(" | ", history.TakeLast(6).Select(h => h.Role + ": " + h.Content));

                var prompt2 = $@"Bạn là trợ lý ảo của cửa hàng Điện máy xanh. 
Dựa trên lịch sử: {shortHistory}
Và danh sách sản phẩm (JSON) dưới đây, hãy trả lời 1-2 câu tiếng Việt ngắn gọn, thân thiện.

{productsJson}

YÊU CẦU: Chỉ trả VĂN BẢN (text), KHÔNG trả JSON.";

                string finalMessage;
                try
                {
                    var body2 = new { contents = new[] { new { parts = new[] { new { text = prompt2 } } } } };
                    var resp2 = await client.PostAsync(url, new StringContent(JsonSerializer.Serialize(body2), Encoding.UTF8, "application/json"));
                    var txt2 = await resp2.Content.ReadAsStringAsync();

                    if (resp2.IsSuccessStatusCode)
                    {
                        var doc2 = JsonDocument.Parse(txt2);
                        finalMessage = doc2.RootElement
                            .GetProperty("candidates")[0]
                            .GetProperty("content")
                            .GetProperty("parts")[0]
                            .GetProperty("text").GetString() ?? string.Empty;
                    }
                    else
                    {
                        finalMessage = productsForClient.Any()
                            ? $"Mình tìm thấy {productsForClient.Count} sản phẩm phù hợp cho bạn."
                            : "Mình chưa tìm thấy sản phẩm phù hợp.";
                    }
                }
                catch
                {
                    finalMessage = productsForClient.Any()
                        ? $"Mình tìm thấy {productsForClient.Count} sản phẩm phù hợp cho bạn."
                        : "Mình chưa tìm thấy sản phẩm phù hợp.";
                }

                // Lưu vào lịch sử với separator + products JSON
                var separator = "|||";
                var modelContent = finalMessage + "\n" + separator + "\n" + productsJson;
                history.Add(new ChatMessage { Role = "model", Content = modelContent, Timestamp = DateTime.Now });
                HttpContext.Session.SetObjectAsJson(ChatHistoryKey, history);

                return Ok(new { message = finalMessage, products = productsForClient });
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

    public class ProductFilterDto
    {
        public bool IsProductQuery { get; set; }
        public List<string>? Keywords { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool Cheapest { get; set; }
        public bool MostExpensive { get; set; }
        public List<int>? CategoryIds { get; set; }
        public List<int>? BrandIds { get; set; }
        public int? Limit { get; set; }
        public string? Message { get; set; }
    }

    public class ChatRequest1
    {
        public string Message { get; set; } = "";
    }
}