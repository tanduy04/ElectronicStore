namespace ElectronicStore.Api.Models
{
    public class HybridChatResponse
    {
        public string message { get; set; } = string.Empty;
        public List<RetrievedContext> RetrievedContexts { get; set; } = new();
        public List<ProductInfo>? Products { get; set; }
        public bool IsProductQuery { get; set; }
        public double ProcessingTimeMs { get; set; }
    }
    public class ProductInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
    }
}
