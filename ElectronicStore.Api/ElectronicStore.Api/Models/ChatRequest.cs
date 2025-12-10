namespace ElectronicStore.Api.Models
{
    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }
    public class ChatResponse
    {
        public string Answer { get; set; } = string.Empty;
        public List<RetrievedContext> RetrievedContexts { get; set; } = new();
        public double ProcessingTimeMs { get; set; }
    }

    public class RetrievedContext
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public double Score { get; set; }
    }
    public class QADocument
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
