using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Helper;
using ElectronicStore.Api.Repositories.Interfaces;
using ElectronicStore.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStore.Api.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ElectronicStoreContext _context;
        private readonly IConfiguration _config;
        private readonly EmailService _emailService;

        public OrderService(IUnitOfWork unitOfWork, ElectronicStoreContext context, IConfiguration config, EmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _config = config;
            _emailService = emailService;
        }

        private string GetBaseUrl() => _config["AppSettings:BaseUrl"];

        public async Task<(bool Success, string Message, object? Data)> GetAllOrdersAsync(int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                if (pageNumber <= 0) pageNumber = 1;
                if (pageSize <= 0) pageSize = 10;

                var baseUrl = GetBaseUrl();

                var query = _context.Orders
                    .Include(o => o.OrderDetails)
                    .Include(o => o.Payments)
                    .OrderByDescending(o => o.OrderDate);

                var totalRecords = await query.CountAsync();

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
                        PaymentStatus = o.Payments.FirstOrDefault().Status,
                        CustomerName = o.FullName,
                        DiscountByVoucher = o.DiscountVoucher,
                        DiscountByPoint = o.DiscountPoint,
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

                var result = new
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
                    Data = orders
                };

                return (true, "Success", result);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> FilterOrdersAsync(string status, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                if (pageNumber <= 0) pageNumber = 1;
                if (pageSize <= 0) pageSize = 10;

                var baseUrl = GetBaseUrl();

                var query = _context.Orders
                    .Include(o => o.OrderDetails)
                    .Include(o => o.Payments)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(o => o.Status == status);
                }

                query = query.OrderByDescending(o => o.OrderDate);

                var totalRecords = await query.CountAsync();

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
                        DiscountByVoucher = o.DiscountVoucher,
                        DiscountByPoint = o.DiscountPoint,
                        PaymentStatus = o.Payments.FirstOrDefault().Status,
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

                var result = new
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
                    Data = orders
                };

                return (true, "Success", result);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> GetOrderByOrderCodeAsync(string orderCode)
        {
            try
            {
                var baseUrl = GetBaseUrl();

                var order = await _context.Orders
                    .Include(o => o.Payments)
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(o => o.OrderCode == orderCode);

                if (order == null)
                    return (false, "Order not found", null);

                var orderDto = new OrderDto
                {
                    OrderCode = order.OrderCode,
                    OrderDate = order.OrderDate,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status,
                    shippingAddress = order.ShippingAddress,
                    PhoneNumber = order.PhoneNumber,
                    paymentMethod = order.PaymentMethod,
                    CustomerName = order.FullName,
                    DiscountByVoucher = order.DiscountVoucher,
                    DiscountByPoint = order.DiscountPoint,
                    PaymentStatus = order.Payments.FirstOrDefault()?.Status,
                    OrderDetails = order.OrderDetails.Select(d => new OrderDetailDto
                    {
                        OrderDetailId = d.OrderDetailId,
                        ProductName = d.Product.ProductName,
                        ProductImage = $"{baseUrl}{_config["ImageSettings:ProductPath"]}{_context.ProductImages.FirstOrDefault(x => x.ProductId == d.ProductId && x.ImageMain == true)?.UrlProductImage}",
                        Quantity = d.Quantity,
                        Price = d.UnitPrice,
                    }).ToList()
                };

                return (true, "Success", orderDto);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> GetOrdersByCustomerAccountIdAsync(int accountId)
        {
            try
            {
                var baseUrl = GetBaseUrl();

                var customerId = await _context.Customers
                    .Where(c => c.AccountId == accountId)
                    .Select(c => c.CustomerId)
                    .FirstOrDefaultAsync();

                if (customerId == 0)
                    return (false, "Customer not found", null);

                var orders = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .Include(o => o.Payments)
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
                        DiscountByVoucher = o.DiscountVoucher,
                        DiscountByPoint = o.DiscountPoint,
                        PaymentStatus = o.Payments.FirstOrDefault().Status,
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

                return (true, "Success", orders);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> UpdateOrderStatusAsync(string orderCode, string newStatus)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Payments)
                    .Include(o => o.Customer)
                    .ThenInclude(c => c.Account)
                    .FirstOrDefaultAsync(o => o.OrderCode == orderCode);

                if (order == null)
                    return (false, "Order not found");

                var currentStatus = order.Status;
                var validStatuses = new List<string> { "Pending", "Processing", "Shipping", "Delivered" };

                if (!validStatuses.Contains(newStatus))
                    return (false, "Invalid status");

                int currentIndex = validStatuses.IndexOf(currentStatus);
                int newIndex = validStatuses.IndexOf(newStatus);

                if (newIndex != currentIndex + 1)
                    return (false, $"Cannot change status from {currentStatus} to {newStatus} directly");

                order.Status = newStatus;
                _context.Orders.Update(order);

                if (newStatus == "Delivered" && order.PaymentMethod == "COD")
                {
                    var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderCode == order.OrderCode);
                    if (payment != null)
                    {
                        payment.Status = "Paid";
                        _context.Payments.Update(payment);
                    }

                    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == order.CustomerId);
                    if (customer != null)
                    {
                        customer.Point = customer.Point + (int)(order.TotalAmount / 1000000);
                        _context.Customers.Update(customer);
                    }
                }

                await _context.SaveChangesAsync();

                if (order.Customer?.Account != null)
                    _emailService.UpdateOrderStatus(order.Customer.Account.Email, order.OrderCode, newStatus);

                return (true, "Order status updated successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating order status: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> CancelOrderAsync(string orderCode, string? role = null, int? accountId = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                Order? order = null;

                if (role == "Customer" && accountId.HasValue)
                {
                    order = await _context.Orders
                        .Include(o => o.Customer)
                        .ThenInclude(c => c.Account)
                        .FirstOrDefaultAsync(o => o.OrderCode == orderCode && o.Customer.AccountId == accountId.Value);
                }
                else
                {
                    order = await _context.Orders
                        .Include(o => o.Customer)
                        .ThenInclude(c => c.Account)
                        .FirstOrDefaultAsync(o => o.OrderCode == orderCode);
                }

                if (order == null)
                    return (false, "Order not found");

                if (order.Status != "Pending")
                    return (false, "Only orders with 'Pending' status can be cancelled");

                order.Status = "Cancelled";

                var itemInOrder = _context.OrderDetails.Where(od => od.OrderCode == order.OrderCode).ToList();
                foreach (var item in itemInOrder)
                {
                    var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += item.Quantity;
                        _context.Products.Update(product);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (order.Customer?.Account != null)
                    _emailService.UpdateOrderStatus(order.Customer.Account.Email, order.OrderCode, "Cancelled");

                return (true, "Order cancelled successfully");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Error cancelling order: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> RefundOrderAsync(string orderCode)
        {
            try
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderCode == orderCode && o.Status == "Cancelled");
                if (order == null)
                    return (false, "Order not found");

                var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderCode == order.OrderCode);
                if (payment == null)
                    return (false, "Payment not found");

                if (payment.Status != "Paid")
                    return (false, "Only paid orders can be refunded");

                payment.Status = "Refunded";
                _context.Payments.Update(payment);
                await _context.SaveChangesAsync();

                return (true, "Order refunded successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error processing refund: {ex.Message}");
            }
        }
    }
}
