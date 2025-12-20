namespace ElectronicStore.Api.Dto
{
    public class ProductChatbot
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        // Trường dùng nội bộ cho Vector Search
        public float[] Embedding { get; set; }
        public decimal Price { get; set; }
    }
}
