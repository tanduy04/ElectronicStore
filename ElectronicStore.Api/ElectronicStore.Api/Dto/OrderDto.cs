namespace ElectronicStore.Api.Dto
{
    public class OrderDetailDto
    {
        public int OrderDetailId { get; set; }
        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
    public class OrderDto
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public string shippingAddress { get; set; }
        public string PhoneNumber { get; set; }
        public string paymentMethod { get; set; }
        public string CustomerName { get; set; }
        public List<OrderDetailDto> OrderDetails { get; set; }
    }

}
