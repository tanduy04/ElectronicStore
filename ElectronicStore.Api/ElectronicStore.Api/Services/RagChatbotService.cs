using ElectronicStore.Api.Models;

namespace ElectronicStore.Api.Services
{
    public class RagChatbotService
    {
        private readonly GeminiService _geminiService;
        private readonly QdrantService _qdrantService;

        public RagChatbotService(GeminiService geminiService, QdrantService qdrantService)
        {
            _geminiService = geminiService;
            _qdrantService = qdrantService;
        }

        public async Task<ChatResponse> ProcessQuestionAsync(string question)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var contexts = await _qdrantService.SearchSimilarAsync(question, topK: 3);
            var answer = await _geminiService.GenerateAnswerAsync(question, contexts);

            stopwatch.Stop();

            return new ChatResponse
            {
                Answer = answer ?? "Xin lỗi, tôi không thể tạo câu trả lời lúc này.",
                RetrievedContexts = contexts,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
    }
}
