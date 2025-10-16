namespace ElectronicStore.Api.Dto
{
    // Đặt trong thư mục DTOs hoặc Models
    public class ChatMessage
    {
        // Xác định ai là người gửi
        public string Role { get; set; } = null!; // Ví dụ: "user", "model"

        // Nội dung tin nhắn
        public string Content { get; set; } = null!;

        // Tùy chọn: Thời gian
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
