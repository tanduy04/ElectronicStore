using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Helper;
using ElectronicStore.Api.Repositories.Interfaces;
using ElectronicStore.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStore.Api.Services
{
    public class CheckoutService : ICheckoutService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ElectronicStoreContext _context;
        private readonly IConfiguration _config;
        private readonly EmailService _mailService;

        public CheckoutService(IUnitOfWork unitOfWork, ElectronicStoreContext context, IConfiguration config, EmailService mailService)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _config = config;
            _mailService = mailService;
        }

        public async Task<(bool Success, string Message, object? Data)> CheckVoucherAsync(string voucherCode, int accountId)
        {
            var voucherUsed = await _context.Orders.FirstOrDefaultAsync(o => o.VoucherCode == voucherCode && o.Customer.AccountId == accountId);
            if (voucherUsed != null && voucherCode != null)
            {
                return (false, "You have used this voucher", null);
            }

            var voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.VoucherCode == voucherCode);

            if (voucher == null)
                return (false, "Voucher not found", null);

            if (!voucher.IsActive || voucher.StartDate > DateTime.Now || voucher.EndDate < DateTime.Now)
                return (false, "Voucher has expired.", null);

            if (voucher.Quantity <= 0)
                return (false, "Voucher is out of stock", null);

            return (true, "Voucher is valid", voucher);
        }

        public async Task<(bool Success, string Message, object? Data)> CheckoutCODAsync(CheckoutCartDto dto, int accountId)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var today = DateOnly.FromDateTime(DateTime.Now);
                var now = TimeOnly.FromDateTime(DateTime.Now);

                // Get cart items
                var cartItems = await _context.Carts.Include(c => c.Product)
                    .Where(c => c.CartId == accountId)
                    .AsNoTracking()
                    .ToListAsync();

                if (!cartItems.Any())
                    return (false, "Empty cart", null);

                // Check stock and apply flash sale prices
                foreach (var item in cartItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null) continue;

                    if (product.StockQuantity < item.Quantity)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return (false, $"Product {product.ProductName} is out of stock", null);
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

                // Check voucher usage
                if (dto.VoucherCode != null)
                {
                    var voucherUsed = await _context.Orders.FirstOrDefaultAsync(o => o.VoucherCode == dto.VoucherCode && o.Customer.AccountId == accountId);
                    if (voucherUsed != null)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return (false, "You have used this voucher", null);
                    }
                }

                // Process customer points
                decimal discountPoint = 0;
                if (dto.usePoint == true)
                {
                    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AccountId == accountId);
                    if (customer == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return (false, "Customer not found.", null);
                    }

                    if (customer.Point <= 0)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return (false, "You have no points to use", null);
                    }

                    discountPoint = customer.Point * 10000;
                    customer.Point = 0;
                    _context.Customers.Update(customer);
                }

                // Generate order code
                string orderCode = await GenerateOrderCodeAsync();

                // Calculate total amount
                decimal totalAmount = cartItems.Sum(c => c.Quantity * c.Product.SellPrice);
                decimal discountVoucher = 0;

                if (dto.VoucherCode != null)
                {
                    var voucher = await _context.Vouchers.FirstOrDefaultAsync(v =>
                        v.VoucherCode == dto.VoucherCode
                        && v.StartDate <= DateTime.Now
                        && v.EndDate >= DateTime.Now
                        && v.IsActive == true
                        && v.Quantity > 0);

                    if (voucher == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return (false, "Invalid voucher code", null);
                    }

                    if (voucher.DiscountType == "percent")
                    {
                        discountVoucher = totalAmount * (voucher.DiscountValue / 100);
                    }
                    else if (voucher.DiscountType == "amount")
                    {
                        discountVoucher = voucher.DiscountValue;
                    }
                }

                totalAmount = totalAmount - discountVoucher - discountPoint;

                // Create order
                var customerId = await _context.Customers
                    .Where(c => c.AccountId == accountId)
                    .Select(c => c.CustomerId)
                    .FirstOrDefaultAsync();

                var order = new Order
                {
                    OrderCode = orderCode,
                    CustomerId = customerId,
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
                    TotalAmount = totalAmount
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Create order details and update stock
                foreach (var item in cartItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null) continue;

                    if (product.StockQuantity < item.Quantity)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return (false, $"Product {product.ProductName} is out of stock", null);
                    }

                    var orderDetail = new OrderDetail
                    {
                        OrderCode = order.OrderCode,
                        ProductId = item.Product.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Product.SellPrice,
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

                    await _context.SaveChangesAsync();
                }

                // Create payment record
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

                // Clear cart
                var cartToRemove = await _context.Carts
                    .Where(c => c.CartId == accountId)
                    .ToListAsync();
                _context.Carts.RemoveRange(cartToRemove);

                await _context.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                // Send email
                var email = await _context.Accounts
                    .Where(a => a.AccountId == accountId)
                    .Select(a => a.Email)
                    .FirstOrDefaultAsync();

                if (email != null)
                    await _mailService.CreateOrderSuccess(email, orderCode);

                return (true, "Order successful", new
                {
                    OrderCode = orderCode,
                    Total = totalAmount,
                    DiscountVoucher = discountVoucher,
                    DiscountPoint = discountPoint
                });
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return (false, $"Error creating order: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> CheckoutVNPayAsync(CheckoutCartDto dto, int accountId, string ipAddress)
        {
            try { await _unitOfWork.BeginTransactionAsync();
            try
            {
                var today = DateOnly.FromDateTime(DateTime.Now);
                var now = TimeOnly.FromDateTime(DateTime.Now);

                // Get cart items
                var cartItems = await _context.Carts
                    .Include(c => c.Product)
                    .Where(c => c.CartId == accountId)
                    .AsNoTracking()
                    .ToListAsync();

                if (!cartItems.Any())
                    return (false, "Cart is empty.", null);

                // Check stock and apply flash sale prices
                foreach (var item in cartItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null) continue;

                    if (product.StockQuantity < item.Quantity)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return (false, $"Product {product.ProductName} is out of stock", null);
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

                // Check voucher usage
                if (dto.VoucherCode != null)
                {
                    var voucherUsed = await _context.Orders.FirstOrDefaultAsync(o => o.VoucherCode == dto.VoucherCode && o.Customer.AccountId == accountId);
                    if (voucherUsed != null)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return (false, "You have used this voucher", null);
                    }
                }

                // Process customer points
                decimal discountPoint = 0;
                if (dto.usePoint == true)
                {
                    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AccountId == accountId);
                    if (customer == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return (false, "Customer not found.", null);
                    }

                    if (customer.Point <= 0)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return (false, "You have no points to use", null);
                    }

                    discountPoint = customer.Point * 1000;
                    customer.Point = 0;
                    _context.Customers.Update(customer);
                }

                // Calculate total amount
                decimal totalAmount = cartItems.Sum(c => c.Quantity * c.Product.SellPrice);
                decimal discountVoucher = 0;

                if (dto.VoucherCode != null)
                {
                    var voucher = await _context.Vouchers.FirstOrDefaultAsync(v =>
                        v.VoucherCode == dto.VoucherCode
                        && v.StartDate <= DateTime.Now
                        && v.EndDate >= DateTime.Now
                        && v.IsActive == true
                        && v.Quantity > 0);

                    if (voucher == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return (false, "Invalid voucher code", null);
                    }

                    if (voucher.DiscountType == "percent")
                    {
                        discountVoucher = totalAmount * (voucher.DiscountValue / 100);
                    }
                    else if (voucher.DiscountType == "amount")
                    {
                        discountVoucher = voucher.DiscountValue;
                    }
                }

                totalAmount = totalAmount - discountVoucher - discountPoint;

                // Create order
                string orderCode = await GenerateOrderCodeAsync();
                var customerId = await _context.Customers
                    .Where(c => c.AccountId == accountId)
                    .Select(c => c.CustomerId)
                    .FirstOrDefaultAsync();

                var order = new Order
                {
                    CustomerId = customerId,
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

                // Create order details and update stock
                foreach (var item in cartItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null) continue;

                    if (product.StockQuantity < item.Quantity)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return (false, $"Product {product.ProductName} is out of stock", null);
                    }

                    var orderDetail = new OrderDetail
                    {
                        OrderCode = order.OrderCode,
                        ProductId = product.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Product.SellPrice,
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

                    await _context.SaveChangesAsync();
                }

                // Clear cart
                var cartToRemove = await _context.Carts
                    .Where(c => c.CartId == accountId)
                    .ToListAsync();
                _context.Carts.RemoveRange(cartToRemove);

                await _context.SaveChangesAsync();

                // Generate VNPay payment URL
                var config = _config.GetSection("VNPay");
                string vnp_Returnurl = config["ReturnUrl"];
                string vnp_Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
                string vnp_TmnCode = config["TmnCode"];
                string vnp_HashSecret = config["HashSecret"];

                VnPayLibrary vnpay = new VnPayLibrary();
                vnpay.AddRequestData("vnp_Version", "2.1.0");
                vnpay.AddRequestData("vnp_Command", "pay");
                vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
                vnpay.AddRequestData("vnp_Amount", ((long)(totalAmount * 100)).ToString());
                vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
                vnpay.AddRequestData("vnp_CurrCode", "VND");
                vnpay.AddRequestData("vnp_IpAddr", ipAddress);
                vnpay.AddRequestData("vnp_Locale", "vn");
                vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang");
                vnpay.AddRequestData("vnp_OrderType", "other");
                vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
                vnpay.AddRequestData("vnp_TxnRef", order.OrderCode);

                await _unitOfWork.CommitTransactionAsync();
                string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);

                return (true, "Payment URL generated", new { PaymentUrl = paymentUrl });
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> ProcessVNPayCallbackAsync(Dictionary<string, string> vnpayData)
        {
            var config = _config.GetSection("VNPay");
            string hashSecret = config["HashSecret"];

            if (!vnpayData.TryGetValue("vnp_SecureHash", out string vnpSecureHash))
                return (false, "Missing signature", null);

            VnPayLibrary vnp = new VnPayLibrary();
            foreach (var item in vnpayData)
            {
                vnp.AddResponseData(item.Key, item.Value);
            }

            bool checkSignature = vnp.ValidateSignature(vnpSecureHash, hashSecret);
            string orderCode = vnp.GetResponseData("vnp_TxnRef");
            var order = await _context.Orders
                .Include(o => o.Customer)
                .ThenInclude(c => c.Account)
                .FirstOrDefaultAsync(o => o.OrderCode == orderCode);

            if (order == null)
                return (false, "Order not found.", null);

            if (checkSignature && vnp.GetResponseData("vnp_ResponseCode") == "00")
            {
                order.Status = "Pending";

                _context.Payments.Add(new Payment
                {
                    OrderCode = order.OrderCode,
                    CustomerId = order.CustomerId,
                    Amount = order.TotalAmount,
                    Method = "VNPay",
                    Status = "Paid",
                    TransactionCode = vnp.GetResponseData("vnp_TransactionNo"),
                    PaymentDate = DateTime.Now
                });

                await _context.SaveChangesAsync();

                var mail = order.Customer?.Account?.Email;
                if (mail != null)
                    await _mailService.CreateOrderSuccess(mail, orderCode);

                return (true, "Payment successful", new
                {
                    Order = new
                    {
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

            return (false, "Payment failed", null);
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

            return $"{today}{nextNumber}";
        }
    }
}
