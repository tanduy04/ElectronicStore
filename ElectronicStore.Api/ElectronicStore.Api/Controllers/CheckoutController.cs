using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace ElectronicStore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CheckoutController : ControllerBase
    {
        private readonly EmailService _mailService;
        private readonly ElectronicStoreContext _context;
        private readonly IConfiguration _config;

        public CheckoutController(ElectronicStoreContext context, IConfiguration config,EmailService mailService)
        {
            _mailService= mailService;
            _context = context;
            _config= config;
        }
        [HttpGet("check-voucher/{voucherCode}")]
        public async Task<IActionResult> CheckVoucher(string voucherCode)
        {
            var voucher = await _context.Vouchers.FirstOrDefaultAsync(v =>
                v.VoucherCode == voucherCode);
            if (voucher == null)
                return NotFound("Voucher not found");
            if(!voucher.IsActive || voucher.StartDate > DateTime.UtcNow || voucher.EndDate < DateTime.UtcNow)
                return BadRequest("Voucher has expired.");
            if(voucher.Quantity <= 0)
                return BadRequest("Voucher is out of stock");
            return Ok("Vouch applied successfully");
        }   
        [HttpPost("cod")]
        [Authorize]
        public async Task<IActionResult> CheckoutCOD(CheckoutCodDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var today = DateOnly.FromDateTime(System.DateTime.Now);
                var now = TimeOnly.FromDateTime(System.DateTime.Now);
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                // 1. Lấy AccountID từ token
                var accountId = User.FindFirst("AccountID")?.Value;
                if (accountId == null) return Unauthorized();

                // 2. Lấy giỏ hàng
                var cartItems = await _context.Carts.Include(c => c.Product)
                    .Where(c => c.CartId == int.Parse(accountId))
                    .AsNoTracking()
                    .ToListAsync();
                foreach (var item in cartItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null) continue;
                    if (product.StockQuantity < item.Quantity)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest($"Product {product.ProductName} is out of stock");
                    }
                    
                    var flashSaleItem = await _context.FlashSaleItems
                        .Include(fsi => fsi.FlashSale)
                        .Where(fsi => fsi.ProductId == item.ProductId &&
                                      fsi.FlashSale.DateSale == today &&
                                      fsi.FlashSale.StartTime <= now &&
                                      fsi.FlashSale.EndTime >= now &&
                                      fsi.Quantity >= item.Quantity)
                        .FirstOrDefaultAsync();
                    if (flashSaleItem != null)
                        item.Product.SellPrice = flashSaleItem.SellPrice;
                }
                var voucherUsed= await _context.Orders.FirstOrDefaultAsync(o => o.VoucherCode == dto.VoucherCode && o.Customer.AccountId == int.Parse(accountId));
                if(voucherUsed != null && dto.VoucherCode != null)
                {
                    return BadRequest("You have used this voucher");
                }
                if (!cartItems.Any())
                    return BadRequest("Empty cart");
                decimal discountPoint = 0;
                if (dto.usePoint == true)
                {
                    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AccountId == int.Parse(accountId));
                    if (customer == null) return BadRequest("Customer not found.");
                    if (customer.Point <= 0) return BadRequest("You have no points to use");
                    discountPoint = customer.Point * 10000;
                    customer.Point = 0;
                    _context.Customers.Update(customer);

                }
                // 3. Tạo OrderCode
                string orderCode = await GenerateOrderCodeAsync();

                // 4. Tính tổng tiền và tạo đơn hàng
                decimal totalAmount = cartItems.Sum(c => c.Quantity * c.Product.SellPrice);
                decimal discountVoucher = 0;
                if (dto.VoucherCode != null)
                {
                    var voucher = _context.Vouchers.FirstOrDefault(v =>
                        v.VoucherCode == dto.VoucherCode
                        && v.StartDate <= DateTime.UtcNow
                        && v.EndDate >= DateTime.UtcNow
                        && v.IsActive == true
                        && v.Quantity > 0
                        )
                        ;
                    if (voucher != null)
                    {
                        if (voucher.DiscountType == "percent")
                        {
                            discountVoucher = totalAmount * (voucher.DiscountValue / 100);
                        }
                        else if (voucher.DiscountType == "amount")
                        {
                            discountVoucher = voucher.DiscountValue;
                        }

                    }
                    else
                    {
                        return BadRequest("Invalid voucher code");
                    }
                }
                totalAmount = totalAmount - discountVoucher - discountPoint;

                var order = new Order
                {
                    OrderCode = orderCode,
                    CustomerId = _context.Customers.FirstOrDefault(c => c.AccountId == int.Parse(accountId)).CustomerId,
                    OrderDate = DateTime.Now,
                    PhoneNumber = dto.PhoneNumber,
                    FullName = dto.FullName,
                    ShippingAddress = dto.Address,
                    Status = "Pending",
                    PaymentMethod = "COD",
                    VoucherCode = dto.VoucherCode,
                    DiscountVoucher = discountVoucher,
                    UsePoint = dto.usePoint,
                    DiscountPoint = discountPoint,
                    // lấy từ khách hàng nếu có
                    TotalAmount = totalAmount
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync(); // để có OrderID

                // 5. Lưu chi tiết đơn hàng (lấy giá từ Product)
                foreach (var item in cartItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (item.Product.StockQuantity < item.Quantity)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest($"Product {item.Product.ProductName} is out of stock");
                    }
                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.OrderId,
                        ProductId = item.Product.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Product.SellPrice,  // Giá tại thời điểm mua
                        TotalPrice = item.Product.SellPrice * item.Quantity
                    };
                     // Cập nhật tồn kho
                    _context.OrderDetails.Add(orderDetail);
                    product.StockQuantity -= item.Quantity;
                    _context.Products.Update(product);
                    var flashSaleItem = await _context.FlashSaleItems
                        .Include(fsi => fsi.FlashSale)
                        .Where(fsi => fsi.ProductId == item.ProductId &&
                                      fsi.FlashSale.DateSale == today &&
                                      fsi.FlashSale.StartTime <= now &&
                                      fsi.FlashSale.EndTime >= now &&
                                      fsi.Quantity >= item.Quantity)
                        .FirstOrDefaultAsync();
                    if (flashSaleItem != null)
                        flashSaleItem.Quantity -= item.Quantity;
                    _context.SaveChanges();

                }


                // 7. Lưu thông tin thanh toán COD
                var payment = new Payment
                {
                    OrderId = order.OrderId,
                    CustomerId = order.CustomerId,
                    Amount = totalAmount,
                    Status = "UnPaid",
                    Method = "COD",
                    TransactionCode = null,
                    PaymentDate = null
                };

                _context.Payments.Add(payment);

                // 8. Xóa giỏ hàng sau khi đặt hàng
                var CartItems = await _context.Carts
                    .Where(c => c.CartId == int.Parse(accountId))
                    .ToListAsync();
                _context.Carts.RemoveRange(CartItems);

                // Lưu tất cả thay đổi
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                await _mailService.CreateOrderSuccess(_context.Accounts.FirstOrDefault(a => a.AccountId == int.Parse(accountId)).Email, orderCode);

                return Ok(new { OrderCode = orderCode, Total = totalAmount, DiscountVoucher = discountVoucher, DiscountPoint = discountPoint, Message = "Order successful" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Error creating order " + ex.Message);
            }
        }
        [HttpPost("CreateVnPayPayment")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateVnPayPayment(CheckoutCodDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var today = DateOnly.FromDateTime(System.DateTime.Now);
                var now = TimeOnly.FromDateTime(System.DateTime.Now);
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                var accountId = User.Claims.FirstOrDefault(c => c.Type == "AccountID")?.Value;
                if (accountId == null) return Unauthorized("Invalid token.");
                var voucherUsed = await _context.Orders.FirstOrDefaultAsync(o => o.VoucherCode == dto.VoucherCode && o.Customer.AccountId == int.Parse(accountId));
                if (voucherUsed != null && dto.VoucherCode != null)
                {
                    return BadRequest("You have used this voucher");
                }
                var cartItems = await _context.Carts
                    .Include(c => c.Product)
                    .Where(c => c.CartId == int.Parse(accountId))
                    .AsNoTracking()
                    .ToListAsync();
                foreach (var item in cartItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null) continue;
                    if (product.StockQuantity < item.Quantity)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest($"Product {product.ProductName} is out of stock");
                    }

                    var flashSaleItem = await _context.FlashSaleItems
                        .Include(fsi => fsi.FlashSale)
                        .Where(fsi => fsi.ProductId == item.ProductId &&
                                      fsi.FlashSale.DateSale == today &&
                                      fsi.FlashSale.StartTime <= now &&
                                      fsi.FlashSale.EndTime >= now &&
                                      fsi.Quantity >= item.Quantity)
                        .FirstOrDefaultAsync();
                    if (flashSaleItem != null)
                        item.Product.SellPrice = flashSaleItem.SellPrice;
                }
                if (!cartItems.Any()) return BadRequest("Cart is empty.");
                decimal discountPoint = 0;
                if (dto.usePoint == true)
                {
                    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AccountId == int.Parse(accountId));
                    if (customer == null) return BadRequest("Customer not found.");
                    if (customer.Point <= 0) return BadRequest("You have no points to use");
                    discountPoint = customer.Point * 1000;
                    customer.Point = 0;
                    _context.Customers.Update(customer);
                }
                decimal totalAmount = cartItems.Sum(c => c.Quantity * c.Product.SellPrice);
                decimal discountVoucher = 0;
                if (dto.VoucherCode != null)
                {
                    var voucher = _context.Vouchers.FirstOrDefault(v =>
                        v.VoucherCode == dto.VoucherCode
                        && v.StartDate <= DateTime.UtcNow
                        && v.EndDate >= DateTime.UtcNow
                        && v.IsActive == true
                        && v.Quantity>0);
                    if (voucher != null)
                    {
                        if (voucher.DiscountType == "percent")
                        {
                            discountVoucher = totalAmount * (voucher.DiscountValue / 100);
                        }
                        else if (voucher.DiscountType == "amount")
                        {
                            discountVoucher = voucher.DiscountValue;
                        }

                    }
                    else
                    {
                        return BadRequest("Invalid voucher code");
                    }
                    totalAmount = totalAmount - discountVoucher - discountPoint;
                }
                // 1. Tạo Order
                string orderCode = await GenerateOrderCodeAsync();
                var order = new Order
                {
                    CustomerId = await _context.Customers
                        .Where(c => c.AccountId == int.Parse(accountId))
                        .Select(c => c.CustomerId)
                        .FirstOrDefaultAsync(),
                    OrderCode = orderCode,
                    OrderDate = DateTime.Now,
                    Status = "Pending",
                    TotalAmount = totalAmount,
                    PaymentMethod = "VNPay",
                    PhoneNumber = dto.PhoneNumber,
                    FullName = dto.FullName,
                    ShippingAddress = dto.Address,
                    VoucherCode = dto.VoucherCode,
                    DiscountVoucher = discountVoucher,
                    UsePoint = dto.usePoint,
                    DiscountPoint = discountPoint
                };
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var item in cartItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null) continue;
                    if (product.StockQuantity < item.Quantity)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest($"Product {product.ProductName} is out of stock");
                    }
                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.OrderId,
                        ProductId = product.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Product.SellPrice,  // Giá tại thời điểm mua
                        TotalPrice = item.Product.SellPrice * item.Quantity
                    };
                    _context.OrderDetails.Add(orderDetail);
                    product.StockQuantity -= item.Quantity;
                    _context.Products.Update(product);
                    var flashSaleItem = await _context.FlashSaleItems
                        .Include(fsi => fsi.FlashSale)
                        .Where(fsi => fsi.ProductId == item.ProductId &&
                                      fsi.FlashSale.DateSale == today &&
                                      fsi.FlashSale.StartTime <= now &&
                                      fsi.FlashSale.EndTime >= now &&
                                      fsi.Quantity >= item.Quantity)
                        .FirstOrDefaultAsync();
                    if (flashSaleItem != null)
                        flashSaleItem.Quantity -= item.Quantity;
                    _context.SaveChanges();
                }
                var Cart = await _context.Carts
                    .Where(c => c.CartId == int.Parse(accountId))
                    .ToListAsync();
                _context.Carts.RemoveRange(Cart);
                await _context.SaveChangesAsync();

                var config = _config.GetSection("VNPay");

                string vnp_Returnurl = config["ReturnUrl"]; // Callback
                string vnp_Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
                string vnp_TmnCode = config["TmnCode"]; // mã merchant
                string vnp_HashSecret = config["HashSecret"]; // secret key

                VnPayLibrary vnpay = new VnPayLibrary();
                vnpay.AddRequestData("vnp_Version", "2.1.0");
                vnpay.AddRequestData("vnp_Command", "pay");
                vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
                vnpay.AddRequestData("vnp_Amount", ((long)(totalAmount * 100)).ToString());
                vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
                vnpay.AddRequestData("vnp_CurrCode", "VND");
                vnpay.AddRequestData("vnp_IpAddr", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
                vnpay.AddRequestData("vnp_Locale", "vn");
                vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang");
                vnpay.AddRequestData("vnp_OrderType", "other");
                vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
                vnpay.AddRequestData("vnp_TxnRef", order.OrderCode);
                await transaction.CommitAsync();
                string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);

                return Ok(new { PaymentUrl = paymentUrl });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }



        [HttpGet("VnPayReturn")]
        public async Task<IActionResult> VnPayReturn([FromQuery] Dictionary<string, string> vnpParams)
        {
            var config = _config.GetSection("VNPay");
            string hashSecret = config["HashSecret"];

            if (!vnpParams.TryGetValue("vnp_SecureHash", out string vnpSecureHash))
                return BadRequest("Missing signature");

            VnPayLibrary vnp = new VnPayLibrary();
            foreach (var item in vnpParams)
            {
                vnp.AddResponseData(item.Key, item.Value);
            }

            bool checkSignature = vnp.ValidateSignature(vnpSecureHash, hashSecret);

            string orderCode = vnp.GetResponseData("vnp_TxnRef");
            var order = await _context.Orders.Include(o => o.Customer).ThenInclude(o => o.Account).FirstOrDefaultAsync(o => o.OrderCode == orderCode);
            if (order == null) return NotFound("Order not found.");

            if (checkSignature && vnp.GetResponseData("vnp_ResponseCode") == "00")
            {
                order.Status = "Pending";

                _context.Payments.Add(new Payment
                {
                    OrderId = order.OrderId,
                    CustomerId = order.CustomerId,
                    Amount = order.TotalAmount,
                    Method = "VNPay",
                    Status = "Paid",
                    TransactionCode = vnp.GetResponseData("vnp_TransactionNo"),
                    PaymentDate = DateTime.Now
                });


                await _context.SaveChangesAsync();
                await _mailService.CreateOrderSuccess(order.Customer.Account.Email, orderCode);

                return Ok(new
                {
                    Message = "Payment successful",
                    Order = new
                    {
                        order.OrderId,
                        order.OrderCode,
                        order.TotalAmount,
                        order.Status,
                        order.DiscountVoucher,
                        order.UsePoint,
                        order.DiscountPoint,
                        PaymentMethod = "VNPay",
                        TransactionCode = vnp.GetResponseData("vnp_TransactionNo"),
                        PaymentDate = DateTime.Now
                    }
                });
            }

            order.Status = "UnPaid";
            await _context.SaveChangesAsync();

            return BadRequest("Payment failed");
        }
        private async Task<string> GenerateOrderCodeAsync()
        {
            var today = DateTime.Now.ToString("ddMMyyyy");
            var lastOrder = await _context.Orders
                .Where(o => o.OrderCode.StartsWith(today))
                .OrderByDescending(o => o.OrderCode)
                .FirstOrDefaultAsync();

            int nextNumber = 1001;
            if (lastOrder != null)
            {
                string lastNumberStr = lastOrder.OrderCode.Substring(8);
                if (int.TryParse(lastNumberStr, out int lastNumber))
                    nextNumber = lastNumber + 1;
            }

            var orderCode = $"{today}{nextNumber}";
            return orderCode;
        }


    }
}
