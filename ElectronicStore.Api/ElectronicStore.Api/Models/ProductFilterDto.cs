namespace ElectronicStore.Api.Models
{
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
}
