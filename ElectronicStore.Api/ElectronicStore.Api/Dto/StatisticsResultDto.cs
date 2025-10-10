namespace ElectronicStore.Api.Dto
{
    public class StatisticsResultDto
    {
        public string Type { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal Profit { get; set; }
        public string Period { get; set; } // ví dụ: "2025-10-10" hoặc "10/2025" hoặc "2025"
    }

}
