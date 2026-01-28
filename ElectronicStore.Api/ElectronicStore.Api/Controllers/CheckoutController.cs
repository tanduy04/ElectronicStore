using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Helper;
using ElectronicStore.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CheckoutController : ControllerBase
    {
        private readonly ICheckoutService _checkoutService;

        public CheckoutController(ICheckoutService checkoutService)
        {
            _checkoutService = checkoutService;
        }
        [Authorize]
        [HttpGet("check-voucher/{voucherCode}")]
        public async Task<IActionResult> CheckVoucher(string voucherCode)
        {
            var accountId = User.FindFirst("AccountID")?.Value;
            if (accountId == null) return Unauthorized();

            var result = await _checkoutService.CheckVoucherAsync(voucherCode, int.Parse(accountId));
            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }
        [HttpPost("cod")]
        [Authorize]
        public async Task<IActionResult> CheckoutCOD(CheckoutCartDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var accountId = User.FindFirst("AccountID")?.Value;
            if (accountId == null) return Unauthorized();

            var result = await _checkoutService.CheckoutCODAsync(dto, int.Parse(accountId));
            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }
        [HttpPost("CreateVnPayPayment")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateVnPayPayment(CheckoutCartDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var accountId = User.Claims.FirstOrDefault(c => c.Type == "AccountID")?.Value;
            if (accountId == null) return Unauthorized("Invalid token.");

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var result = await _checkoutService.CheckoutVNPayAsync(dto, int.Parse(accountId), ipAddress);
            
            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }

        [HttpPost("Payment-without-login")]
        public async Task<IActionResult> ByNow(CheckoutProductDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                // lay thong tin khach hang
                var customerExist = await _context.Customers.FirstOrDefaultAsync(c => c.Phone == dto.PhoneNumber);
                if (customerExist == null)
                {
                    var newcustomer = new Customer();
                    newcustomer.AccountId = null;
                    newcustomer.FullName = dto.FullName;
                    newcustomer.Phone = dto.PhoneNumber;
                    newcustomer.Address = dto.Address;
                    newcustomer.Point = 0;
                    newcustomer.Address = dto.Address;
                    newcustomer.CreatedAt = DateTime.Now;
                    _context.Customers.Add(newcustomer);
                    await _context.SaveChangesAsync();
                }
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Phone == dto.PhoneNumber);



                if (!dto.Products.Any())
                    return BadRequest("Empty cart");
                var today = DateOnly.FromDateTime(System.DateTime.Now);
                var now = TimeOnly.FromDateTime(System.DateTime.Now);
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                // 1. Lấy AccountID từ token
                decimal totalAmount = 0;
                foreach (var item in dto.Products)
                {
                    var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == item.ProductId);
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
                        product.SellPrice = flashSaleItem.SellPrice;
                    totalAmount += item.Quantity * product.SellPrice;
                }
                var voucherUsed = await _context.Orders.FirstOrDefaultAsync(o => o.VoucherCode == dto.VoucherCode && o.CustomerId == customer.CustomerId);
                if (voucherUsed != null && dto.VoucherCode != null)
                {
                    return BadRequest("You have used this voucher");
                }

                decimal discountPoint = 0;
                if (dto.usePoint == true)
                {
                    if (customer.Point <= 0) return BadRequest("You have no points to use");
                    discountPoint = customer.Point * 10000;
                    customer.Point = 0;
                    _context.Customers.Update(customer);

                }
                // 3. Tạo OrderCode
                string orderCode = await GenerateOrderCodeAsync();

                // 4. Tính tổng tiền và tạo đơn hàng

                decimal discountVoucher = 0;
                if (dto.VoucherCode != null)
                {
                    var voucher = _context.Vouchers.FirstOrDefault(v =>
                        v.VoucherCode == dto.VoucherCode
                        && v.StartDate <= DateTime.Now
                        && v.EndDate >= DateTime.Now
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
                ///// coppy

                if (dto.method == "COD")
                {


                    var order = new Order
                    {
                        OrderCode = orderCode,
                        CustomerId = customer.CustomerId,
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

                    foreach (var productItem in dto.Products)
                    {
                        var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == productItem.ProductId);
                        if (product.StockQuantity < productItem.Quantity)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest($"Product {product.ProductName} is out of stock");
                        }
                        var flashSaleItem = await _context.FlashSaleItems
                        .Include(fsi => fsi.FlashSale)
                        .Where(fsi => fsi.ProductId == productItem.ProductId &&
                                      fsi.FlashSale.DateSale == today &&
                                      fsi.FlashSale.StartTime <= now &&
                                      fsi.FlashSale.EndTime >= now &&
                                      fsi.Quantity >= productItem.Quantity)
                        .FirstOrDefaultAsync();
                        if (flashSaleItem != null)
                            product.SellPrice = flashSaleItem.SellPrice;
                        var orderDetail = new OrderDetail
                        {
                            OrderCode = order.OrderCode,
                            ProductId = productItem.ProductId,
                            Quantity = productItem.Quantity,
                            UnitPrice = product.SellPrice,  // Giá tại thời điểm mua
                            TotalPrice = product.SellPrice * productItem.Quantity
                        };
                        var productToUpdate = await _context.Products.FindAsync(productItem.ProductId);
                        // Cập nhật tồn kho
                        _context.OrderDetails.Add(orderDetail);
                        productToUpdate.StockQuantity -= productItem.Quantity;
                        _context.Products.Update(productToUpdate);

                        if (flashSaleItem != null)
                            flashSaleItem.Quantity -= productItem.Quantity;
                        _context.SaveChanges();
                    }





                    // 7. Lưu thông tin thanh toán COD
                    var payment = new Payment
                    {
                        OrderCode = order.OrderCode,
                        CustomerId = order.CustomerId,
                        Amount = totalAmount,
                        Status = "UnPaid",
                        Method = "COD",
                        TransactionCode = null,
                        PaymentDate = null
                    };

                    _context.Payments.Add(payment);



                    // Lưu tất cả thay đổi
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    var mail = customer?.Account?.Email;
                    if (mail != null)
                        await _mailService.CreateOrderSuccess(mail, orderCode);

                    return Ok(new { OrderCode = orderCode, Total = totalAmount, DiscountVoucher = discountVoucher, DiscountPoint = discountPoint, Message = "Order successful" });
                }
                else
                {
                    var order = new Order
                    {
                        CustomerId = customer.CustomerId,
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



                    foreach (var productItem in dto.Products)
                    {
                        var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == productItem.ProductId);
                        if (product.StockQuantity < productItem.Quantity)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest($"Product {product.ProductName} is out of stock");
                        }
                        var flashSaleItem = await _context.FlashSaleItems
                        .Include(fsi => fsi.FlashSale)
                        .Where(fsi => fsi.ProductId == productItem.ProductId &&
                                      fsi.FlashSale.DateSale == today &&
                                      fsi.FlashSale.StartTime <= now &&
                                      fsi.FlashSale.EndTime >= now &&
                                      fsi.Quantity >= productItem.Quantity)
                        .FirstOrDefaultAsync();
                        if (flashSaleItem != null)
                            product.SellPrice = flashSaleItem.SellPrice;
                        var orderDetail = new OrderDetail
                        {
                            OrderCode = order.OrderCode,
                            ProductId = productItem.ProductId,
                            Quantity = productItem.Quantity,
                            UnitPrice = product.SellPrice,  // Giá tại thời điểm mua
                            TotalPrice = product.SellPrice * productItem.Quantity
                        };
                        var productToUpdate = await _context.Products.FindAsync(productItem.ProductId);
                        // Cập nhật tồn kho
                        _context.OrderDetails.Add(orderDetail);
                        productToUpdate.StockQuantity -= productItem.Quantity;
                        _context.Products.Update(productToUpdate);

                        if (flashSaleItem != null)
                            flashSaleItem.Quantity -= productItem.Quantity;
                        _context.SaveChanges();
                    }


                    await _context.SaveChangesAsync();

                    var config = _config.GetSection("VNPay");

                    string vnp_Returnurl =config["ReturnUrl"];// Callback
                    Console.WriteLine($"[VNPay] ReturnUrl from DTO: {dto.ReturnUrl ?? "NULL"}");
                    Console.WriteLine($"[VNPay] Final ReturnUrl used: {vnp_Returnurl}");
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
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Error creating order " + ex.Message);
            }
        }

        [HttpGet("VnPayReturn")]
        public async Task<IActionResult> VnPayReturn([FromQuery] Dictionary<string, string> vnpParams)
        {
            var result = await _checkoutService.ProcessVNPayCallbackAsync(vnpParams);
            
            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }

    }
}
