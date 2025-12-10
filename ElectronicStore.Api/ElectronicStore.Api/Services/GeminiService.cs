using System.Text;
using System.Text.Json;
using ElectronicStore.Api.Models;

namespace ElectronicStore.Api.Services
{
    public class GeminiService
    {
        private const string ApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/";
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<GeminiService> _logger;

        public GeminiService(IConfiguration configuration, ILogger<GeminiService> logger)
        {
            _apiKey = configuration["Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini API Key");
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _logger = logger;
        }

        // ========== EMBEDDING ==========
        public async Task<List<double>?> GetEmbeddingAsync(string text)
        {
            try
            {
                string url = $"{ApiBaseUrl}models/gemini-embedding-001:embedContent?key={_apiKey}";

                var requestBody = new
                {
                    content = new
                    {
                        parts = new[] { new { text = text } }
                    }
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(responseBody);

                    var embeddingValues = doc.RootElement
                        .GetProperty("embedding")
                        .GetProperty("values")
                        .EnumerateArray();

                    return embeddingValues.Select(v => v.GetDouble()).ToList();
                }
                else
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Embedding API error: {StatusCode} - {Error}",
                        response.StatusCode, errorBody);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetEmbeddingAsync");
                return null;
            }
        }

        // ========== GENERATE ANSWER (với RAG Context) ==========
        public async Task<string?> GenerateAnswerAsync(string question, List<RetrievedContext> contexts)
        {
            try
            {
                var models = new[]
                {
                    "gemini-2.5-flash",
                    "gemini-2.5-flash-lite",
                    "gemini-robotics-er-1.5-preview"
                };

                foreach (var model in models)
                {
                    _logger.LogInformation("Trying model: {Model}", model);

                    var result = await TryGenerateWithModel(model, question, contexts);

                    if (result != null)
                    {
                        _logger.LogInformation("Successfully generated answer with model: {Model}", model);
                        return result;
                    }
                }

                _logger.LogError("All models failed to generate answer");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GenerateAnswerAsync");
                return null;
            }
        }

        private async Task<string?> TryGenerateWithModel(string modelName, string question, List<RetrievedContext> contexts)
        {
            try
            {
                string url = $"{ApiBaseUrl}models/{modelName}:generateContent?key={_apiKey}";

                var contextText = string.Join("\n\n", contexts.Select((ctx, idx) =>
                    $"Context {idx + 1} (Score: {ctx.Score:F4}):\nQ: {ctx.Question}\nA: {ctx.Answer}"));

                var prompt = $@"Bạn là một trợ lý AI thông minh. Dựa trên các thông tin context bên dưới, hãy trả lời câu hỏi của người dùng một cách chính xác và tự nhiên.

CÁC CONTEXT LIÊN QUAN:
{contextText}

CÂU HỎI CỦA NGƯỜI DÙNG:
{question}

HƯỚNG DẪN:
- Sử dụng thông tin từ context để trả lời
- Nếu context không đủ thông tin, hãy nói rõ điều đó
- Trả lời ngắn gọn, rõ ràng bằng tiếng Việt
- Không bịa đặt thông tin không có trong context

TRẢ LỜI:";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        topP = 0.95,
                        maxOutputTokens = 1024
                    }
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                _logger.LogDebug("Sending request to: {Url}", url);

                var response = await _httpClient.PostAsync(url, content);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Response: {Response}", responseBody);

                    using JsonDocument doc = JsonDocument.Parse(responseBody);

                    if (!doc.RootElement.TryGetProperty("candidates", out var candidates) ||
                        candidates.GetArrayLength() == 0)
                    {
                        _logger.LogWarning("No candidates in response from {Model}", modelName);
                        return null;
                    }

                    var text = candidates[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    return text;
                }
                else
                {
                    _logger.LogWarning("Model {Model} failed: {StatusCode} - {Error}",
                        modelName, response.StatusCode, responseBody);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error with model {Model}", modelName);
                return null;
            }
        }

        // ========== GENERATE RAW TEXT (cho Intent Analysis & Product Response) ==========
        public async Task<string?> CallGeminiRawAsync(string prompt, string? preferredModel = null)
        {
            try
            {
                var models = string.IsNullOrEmpty(preferredModel)
                    ? new[] { "gemini-2.5-flash", "gemini-2.5-flash-lite", "gemini-robotics-er-1.5-preview" }
                    : new[] { preferredModel };

                foreach (var model in models)
                {
                    _logger.LogInformation("Calling Gemini with model: {Model}", model);

                    string url = $"{ApiBaseUrl}models/{model}:generateContent?key={_apiKey}";

                    var requestBody = new
                    {
                        contents = new[]
                        {
                            new
                            {
                                parts = new[]
                                {
                                    new { text = prompt }
                                }
                            }
                        },
                        generationConfig = new
                        {
                            temperature = 0.7,
                            topP = 0.95,
                            maxOutputTokens = 2048
                        }
                    };

                    var content = new StringContent(
                        JsonSerializer.Serialize(requestBody),
                        Encoding.UTF8,
                        "application/json"
                    );

                    var response = await _httpClient.PostAsync(url, content);
                    string responseBody = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        using JsonDocument doc = JsonDocument.Parse(responseBody);

                        if (!doc.RootElement.TryGetProperty("candidates", out var candidates) ||
                            candidates.GetArrayLength() == 0)
                        {
                            _logger.LogWarning("No candidates in response from {Model}", model);
                            continue;
                        }

                        var text = candidates[0]
                            .GetProperty("content")
                            .GetProperty("parts")[0]
                            .GetProperty("text")
                            .GetString();

                        _logger.LogInformation("Successfully generated raw text with model: {Model}", model);
                        return text;
                    }
                    else
                    {
                        _logger.LogWarning("Model {Model} failed: {StatusCode} - {Error}",
                            model, response.StatusCode, responseBody);
                    }
                }

                _logger.LogError("All models failed for raw generation");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in CallGeminiRawAsync");
                return null;
            }
        }
    }
}