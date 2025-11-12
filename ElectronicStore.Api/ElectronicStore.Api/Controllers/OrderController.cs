using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ElectronicStore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly EmailService _emailService;
        private readonly IConfiguration _config;
        private readonly ElectronicStoreContext _context;

        public OrderController(ElectronicStoreContext context, IConfiguration config, EmailService emailService)
        {
            _emailService= emailService;
            _config = config;
            _context = context;
        }
        private string GetBaseUrl() => $"{Request.Scheme}://{Request.Host}/";

        //=================== ADMIN & EMPLOYEE ===================

        // Lấy tất cả đơn hàng
        [HttpGet("getAll")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> GetAll(int pageNumber = 1, int pageSize = 10)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            var baseUrl = GetBaseUrl();

            // Query gốc
            var query = _context.Orders
                .Include(o => o.OrderDetails)
                .OrderByDescending(o => o.OrderDate);

            // Tổng số bản ghi
            var totalRecords = await query.CountAsync();

            // Lấy dữ liệu phân trang
            var orders = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrderDto
                {
                    OrderCode = o.OrderCode,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    shippingAddress = o.ShippingAddress,
                    PhoneNumber = o.PhoneNumber,
                    paymentMethod = o.PaymentMethod,
                    CustomerName = o.FullName,
                    OrderDetails = o.OrderDetails.Select(d => new OrderDetailDto
                    {
                        OrderDetailId = d.OrderDetailId,
                        ProductName = d.Product.ProductName,
                        ProductImage = $"{baseUrl}{_config["ImageSettings:ProductPath"]}{_context.ProductImages.FirstOrDefault(x => x.ProductId == d.ProductId && x.ImageMain == true).UrlProductImage}",
                        Quantity = d.Quantity,
                        Price = d.UnitPrice,
                    }).ToList()
                })
                .ToListAsync();

            // Trả về dữ liệu kèm meta thông tin phân trang
            return Ok(new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
                Data = orders
            });
        }
        [HttpGet("filter")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> FilterOrders(string status, int pageNumber = 1, int pageSize = 10)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            var baseUrl = GetBaseUrl();

            // Query gốc
            var query = _context.Orders
                .Include(o => o.OrderDetails)
                .AsQueryable();

            // Lọc theo trạng thái nếu có
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            query = query.OrderByDescending(o => o.OrderDate);

            // Tổng số bản ghi sau khi lọc
            var totalRecords = await query.CountAsync();

            // Lấy dữ liệu phân trang
            var orders = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrderDto
                {
                    OrderCode = o.OrderCode,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    shippingAddress = o.ShippingAddress,
                    PhoneNumber = o.PhoneNumber,
                    paymentMethod = o.PaymentMethod,
                    CustomerName = o.FullName,
                    OrderDetails = o.OrderDetails.Select(d => new OrderDetailDto
                    {
                        OrderDetailId = d.OrderDetailId,
                        ProductName = d.Product.ProductName,
                        ProductImage = $"{baseUrl}{_config["ImageSettings:ProductPath"]}{_context.ProductImages.FirstOrDefault(x => x.ProductId == d.ProductId && x.ImageMain == true).UrlProductImage}",
                        Quantity = d.Quantity,
                        Price = d.UnitPrice,
                    }).ToList()
                })
                .ToListAsync();

            // Trả về dữ liệu kèm meta thông tin phân trang
            return Ok(new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
                Data = orders
            });
        }


        // Lấy đơn hàng theo OrderCode
        [HttpGet("getByOrderCode/{orderCode}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> GetByOrderCode(string orderCode)
        {
            var baseUrl = GetBaseUrl();

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderCode == orderCode);

            if (order == null) return NotFound("Order not found");

            return Ok(new OrderDto
            {
                OrderCode = order.OrderCode,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                shippingAddress = order.ShippingAddress,
                PhoneNumber = order.PhoneNumber,
                paymentMethod = order.PaymentMethod,
                CustomerName = order.FullName,
                OrderDetails = order.OrderDetails.Select(d => new OrderDetailDto
                {
                    OrderDetailId = d.OrderDetailId,
                    ProductName = d.Product.ProductName,
                    ProductImage = $"{baseUrl}{_config["ImageSettings:ProductPath"]}{_context.ProductImages.FirstOrDefault(x => x.ProductId == d.ProductId && x.ImageMain == true).UrlProductImage}",
                    Quantity = d.Quantity,
                    Price = d.UnitPrice,
                }).ToList()
            });
        }

       


        // =================== CUSTOMER ===================

        // Lấy đơn hàng theo customerId từ token
        [HttpGet("getByCustomer")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetByCustomer()
        {
            var baseUrl = GetBaseUrl();

            var accountId = User.Claims.FirstOrDefault(c => c.Type == "AccountID")?.Value;
            if (accountId == null) return Unauthorized("Invalid token");

            // Tìm customerId từ accountId
            var customerId = await _context.Customers
                .Where(c => c.AccountId == int.Parse(accountId))
                .Select(c => c.CustomerId)
                .FirstOrDefaultAsync();

            if (customerId == 0) return NotFound("Customer not found");

            // Lấy danh sách đơn hàng của customer
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new OrderDto
                {
                    OrderCode = o.OrderCode,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    shippingAddress = o.ShippingAddress,
                    PhoneNumber = o.PhoneNumber,
                    paymentMethod = o.PaymentMethod,
                    CustomerName = o.FullName,  // giả sử bảng Customer có cột Name
                    OrderDetails = o.OrderDetails.Select(d => new OrderDetailDto
                    {
                        OrderDetailId = d.OrderDetailId,
                        ProductName = d.Product.ProductName,
                        ProductImage = $"{baseUrl}{_config["ImageSettings:ProductPath"]}{_context.ProductImages.FirstOrDefault(x => x.ProductId == d.ProductId && x.ImageMain == true).UrlProductImage}",
                        Quantity = d.Quantity,
                        Price = d.UnitPrice,
                    }).ToList()
                })
                .ToListAsync();

            return Ok(orders);
        }
        [HttpPut("update-status/{OrderCode}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> UpdateOrderStatus(string OrderCode, [FromBody] string newStatus)
        {
            try
            {
                var order = await _context.Orders.Include(o => o.Payments).Include(o => o.Customer).ThenInclude(ac => ac.Account)
                .FirstOrDefaultAsync(o => o.OrderCode == OrderCode);
                if (order == null) return NotFound("Order not found");

                var currentStatus = order.Status;
                var validStatuses = new List<string> { "Pending", "Processing", "Shipping", "Delivered" };

                if (!validStatuses.Contains(newStatus))
                    return BadRequest("Invalid status");

                int currentIndex = validStatuses.IndexOf(currentStatus);
                int newIndex = validStatuses.IndexOf(newStatus);

                if (newIndex != currentIndex + 1)
                    return BadRequest($"Cannot change status from {currentStatus} to {newStatus} directly");

                order.Status = newStatus;
                _context.Orders.Update(order);

                if (newStatus == "Delivered" && order.PaymentMethod == "COD")
                {
                    if (order.PaymentMethod == "COD")
                    {
                        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderCode == order.OrderCode);
                        if (payment != null)
                        {
                            payment.Status = "Paid";
                            _context.Payments.Update(payment);
                        }
                    }
                    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == order.CustomerId);
                    if (customer != null)
                    {
                        customer.Point = customer.Point + (int)(order.TotalAmount / 1000000); // 1 điểm cho mỗi 10000đ
                        _context.Customers.Update(customer);
                    }
                }

                await _context.SaveChangesAsync();
                if(order.Customer.Account != null)
                    _emailService.UpdateOrderStatus(order.Customer.Account.Email, order.OrderCode, newStatus);
                return Ok(new { Message = "Order status updated successfully", OrderCode = OrderCode, NewStatus = newStatus });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error updating order status: " + ex.Message);
            }
        }

        [HttpPost("CancelOrder")]
        [Authorize]
        public async Task<IActionResult> CancelOrder(string OrderCode)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                Order order = null;
                if (role == "Customer")
                {
                    var accountId = User.Claims.FirstOrDefault(c => c.Type == "AccountID")?.Value;
                    order = await _context.Orders.Include(o => o.Customer).ThenInclude(ac => ac.Account).FirstOrDefaultAsync(o => o.OrderCode == OrderCode && o.Customer.AccountId == int.Parse(accountId));
                }
                else
                {
                    order = await _context.Orders.Include(o => o.Customer).ThenInclude(ac => ac.Account).FirstOrDefaultAsync(o => o.OrderCode == OrderCode);
                }
                if (order == null)
                    return NotFound("Order not found");
                if (order.Status != "Pending")
                    return BadRequest("Only orders with 'Pending' status can be cancelled");


                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.OrderCode == order.OrderCode);

                if (payment == null)
                    return BadRequest("Payment info not found");
                // Nếu thanh toán VNPay và đã thanh toán => Gọi Refund
                //if (order.PaymentMethod == "VNPay" && payment.Status == "Paid")
                //{
                //    var refundResult = await RefundVNPay(order, payment);
                //    if (!refundResult.Success)
                //    {
                //        await transaction.RollbackAsync();
                //        return BadRequest($"Refund failed: {refundResult.Message}");
                //    }

                //    payment.Status = "Refunded";
                //}

                // Cập nhật trạng thái đơn hàng
                order.Status = "Cancelled";
                var itemInOrder = _context.OrderDetails.Where(od => od.OrderCode == order.OrderCode).ToList();
                foreach (var item in itemInOrder)
                {
                    var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += item.Quantity; // Hoặc số lượng bạn muốn hoàn trả
                        _context.Products.Update(product);
                    }
                }
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                if(order.Customer.Account.Email != null)
                _emailService.UpdateOrderStatus(order.Customer.Account.Email, order.OrderCode, "Cancelled");
                return Ok(new { Message = "Order cancelled successfully", order.OrderCode });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Error cancelling order: " + ex.Message);
            }
        }

        private async Task<(bool Success, string Message)> RefundVNPay(Order order, Payment payment)
        {
            var config = _config.GetSection("VNPay");
            string vnp_TmnCode = config["TmnCode"];
            string vnp_HashSecret = config["HashSecret"];
            string vnp_Url = "https://sandbox.vnpayment.vn/merchant_webapi/api/transaction";

            // Sử dụng VnPayLibrary giống như khi thanh toán
            var vnpay = new VnPayLibrary();

            vnpay.AddRequestData("vnp_RequestId", DateTime.Now.Ticks.ToString());
            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "refund");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_TransactionType", "02");
            vnpay.AddRequestData("vnp_TxnRef", order.OrderCode);
            vnpay.AddRequestData("vnp_Amount", ((long)(payment.Amount)).ToString()); // BỎ * 100 - thử hoàn tiền không nhân 100
            vnpay.AddRequestData("vnp_OrderInfo", "Refund"); // Đơn giản hóa, không có space
            vnpay.AddRequestData("vnp_TransactionNo", payment.TransactionCode);
            vnpay.AddRequestData("vnp_TransactionDate", payment.PaymentDate?.ToString("yyyyMMddHHmmss") ?? "");
            vnpay.AddRequestData("vnp_CreateBy", "system");
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_IpAddr", "127.0.0.1"); // Dùng IPv4 thay vì ::1

            // Tạo signature theo cách của VnPayLibrary
            string signData = CreateSignatureData(vnpay.RequestData);
            string secureHash = Utils.HmacSHA512(vnp_HashSecret, signData);

            // Thử tạo signature không URL encode (có thể API refund khác với payment)
            string signDataNoEncode = CreateSignatureDataNoEncode(vnpay.RequestData);
            string secureHashNoEncode = Utils.HmacSHA512(vnp_HashSecret, signDataNoEncode);

            Console.WriteLine("=== DEBUG VNPAY REFUND (DÙNG VNPAY LIBRARY) ===");
            Console.WriteLine($"Sign data (URL encoded): {signData}");
            Console.WriteLine($"Secure hash (URL encoded): {secureHash}");
            Console.WriteLine($"Sign data (NO encode): {signDataNoEncode}");
            Console.WriteLine($"Secure hash (NO encode): {secureHashNoEncode}");

            // Thử dùng signature không encode trước
            string finalSecureHash = secureHashNoEncode;

            // Tạo JSON request
            var requestData = new Dictionary<string, string>();
            foreach (var item in vnpay.RequestData)
            {
                requestData.Add(item.Key, item.Value);
                Console.WriteLine($"  {item.Key} = '{item.Value}'");
            }
            requestData.Add("vnp_SecureHash", finalSecureHash);

            using var httpClient = new HttpClient();
            var jsonData = JsonConvert.SerializeObject(requestData, Formatting.Indented);
            var httpContent = new StringContent(jsonData, Encoding.UTF8, "application/json");

            Console.WriteLine($"JSON gửi đi: {jsonData}");

            try
            {
                var response = await httpClient.PostAsync(vnp_Url, httpContent);
                var content = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Response từ VNPay: {content}");

                var responseObj = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);

                if (responseObj != null && responseObj.TryGetValue("vnp_ResponseCode", out string responseCode))
                {
                    if (responseCode == "00")
                    {
                        return (true, "Hoàn tiền thành công");
                    }
                    else
                    {
                        string message = responseObj.TryGetValue("vnp_Message", out string msg) ? msg : "Lỗi không xác định";
                        return (false, $"Hoàn tiền thất bại: {responseCode} - {message}");
                    }
                }

                return (false, $"Định dạng response không hợp lệ: {content}");
            }
            catch (Exception ex)
            {
                return (false, $"Có lỗi xảy ra: {ex.Message}");
            }
        }

        // Method helper để tạo signature data giống VnPayLibrary
        private string CreateSignatureData(SortedList<string, string> requestData)
        {
            var data = new StringBuilder();
            foreach (var kv in requestData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }

            if (data.Length > 0)
            {
                data.Remove(data.Length - 1, 1); // Remove last '&'
            }

            return data.ToString();
        }

        // Method helper để tạo signature data KHÔNG URL encode
        private string CreateSignatureDataNoEncode(SortedList<string, string> requestData)
        {
            var data = new StringBuilder();
            foreach (var kv in requestData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(kv.Key + "=" + kv.Value + "&");
                }
            }

            if (data.Length > 0)
            {
                data.Remove(data.Length - 1, 1); // Remove last '&'
            }

            return data.ToString();
        }
    }
}
