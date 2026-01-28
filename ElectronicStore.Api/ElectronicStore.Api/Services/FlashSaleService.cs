using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Repositories.Interfaces;
using ElectronicStore.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStore.Api.Services
{
    public class FlashSaleService : IFlashSaleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ElectronicStoreContext _context;
        private readonly IConfiguration _config;

        public FlashSaleService(IUnitOfWork unitOfWork, ElectronicStoreContext context, IConfiguration config)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _config = config;
        }

        private string GetBaseUrl() => _config["AppSettings:BaseUrl"];

        public async Task<(bool Success, string Message, object? Data)> GetAllFlashSalesAsync()
        {
            try
            {
                var baseUrl = GetBaseUrl();
                var flashSales = await _context.FlashSales
                    .Include(fs => fs.FlashSaleItems)
                    .ThenInclude(fs => fs.Product)
                    .ThenInclude(p => p.ProductImages)
                    .Select(fs => new FlashSaleViewDto
                    {
                        FlashSaleId = fs.FlashSaleId,
                        FlashSaleName = fs.FlashSaleName,
                        Description = fs.Description,
                        DateSale = fs.DateSale,
                        StartTime = fs.StartTime,
                        EndTime = fs.EndTime,
                        Items = fs.FlashSaleItems.Select(fsi => new FlashSaleItemViewDto
                        {
                            ItemId = fsi.ItemId,
                            Product = new ProductSaleViewDto
                            {
                                ProductId = fsi.Product.ProductId,
                                ProductName = fsi.Product.ProductName,
                                imageUrl = $"{baseUrl}{_config["ImageSettings:ProductPath"]}{fsi.Product.ProductImages.Where(pi => pi.ImageMain).Select(pi => pi.UrlProductImage).FirstOrDefault()}",
                                OriginalPrice = fsi.Product.OriginalPrice,
                            },
                            Quantity = fsi.Quantity,
                            SellPrice = fsi.SellPrice
                        }).ToList()
                    })
                    .ToListAsync();

                return (true, "Success", flashSales);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> GetFlashSaleByIdAsync(int id)
        {
            try
            {
                var baseUrl = GetBaseUrl();

                var flashSale = await _context.FlashSales
                    .Include(fs => fs.FlashSaleItems)
                    .ThenInclude(fs => fs.Product)
                    .ThenInclude(p => p.ProductImages)
                    .Where(fs => fs.FlashSaleId == id)
                    .Select(fs => new FlashSaleViewDto
                    {
                        FlashSaleId = fs.FlashSaleId,
                        FlashSaleName = fs.FlashSaleName,
                        Description = fs.Description,
                        DateSale = fs.DateSale,
                        StartTime = fs.StartTime,
                        EndTime = fs.EndTime,
                        Items = fs.FlashSaleItems.Select(fsi => new FlashSaleItemViewDto
                        {
                            ItemId = fsi.ItemId,
                            Product = new ProductSaleViewDto
                            {
                                ProductId = fsi.Product.ProductId,
                                ProductName = fsi.Product.ProductName,
                                imageUrl = $"{baseUrl}{_config["ImageSettings:ProductPath"]}{fsi.Product.ProductImages.Where(pi => pi.ImageMain).Select(pi => pi.UrlProductImage).FirstOrDefault()}",
                                OriginalPrice = fsi.Product.OriginalPrice,
                            },
                            Quantity = fsi.Quantity,
                            SellPrice = fsi.SellPrice
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (flashSale == null)
                    return (false, "Flash sale not found.", null);

                return (true, "Success", flashSale);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> GetFlashSaleTodayAndTomorrowAsync()
        {
            try
            {
                var baseUrl = GetBaseUrl();
                var today = DateOnly.FromDateTime(DateTime.Now);
                var tomorrow = today.AddDays(1);
                var now = TimeOnly.FromDateTime(DateTime.Now);

                var flashSalesToday = await _context.FlashSales
                    .Include(fs => fs.FlashSaleItems)
                    .ThenInclude(fs => fs.Product)
                    .ThenInclude(p => p.ProductImages)
                    .Where(fs => fs.DateSale == today && fs.EndTime > now)
                    .Select(fs => new FlashSaleViewDto
                    {
                        FlashSaleId = fs.FlashSaleId,
                        FlashSaleName = fs.FlashSaleName,
                        Description = fs.Description,
                        DateSale = fs.DateSale,
                        StartTime = fs.StartTime,
                        EndTime = fs.EndTime,
                        Items = fs.FlashSaleItems.Select(fsi => new FlashSaleItemViewDto
                        {
                            ItemId = fsi.ItemId,
                            Product = new ProductSaleViewDto
                            {
                                ProductId = fsi.Product.ProductId,
                                ProductName = fsi.Product.ProductName,
                                imageUrl = $"{baseUrl}{_config["ImageSettings:ProductPath"]}{fsi.Product.ProductImages.Where(pi => pi.ImageMain).Select(pi => pi.UrlProductImage).FirstOrDefault()}",
                                OriginalPrice = fsi.Product.OriginalPrice,
                            },
                            Quantity = fsi.Quantity,
                            SellPrice = fsi.SellPrice
                        }).ToList()
                    })
                    .ToListAsync();

                var flashSalesTomorrow = await _context.FlashSales
                    .Include(fs => fs.FlashSaleItems)
                    .ThenInclude(fs => fs.Product)
                    .ThenInclude(p => p.ProductImages)
                    .Where(fs => fs.DateSale == tomorrow)
                    .Select(fs => new FlashSaleViewDto
                    {
                        FlashSaleId = fs.FlashSaleId,
                        FlashSaleName = fs.FlashSaleName,
                        Description = fs.Description,
                        DateSale = fs.DateSale,
                        StartTime = fs.StartTime,
                        EndTime = fs.EndTime,
                        Items = fs.FlashSaleItems.Select(fsi => new FlashSaleItemViewDto
                        {
                            ItemId = fsi.ItemId,
                            Product = new ProductSaleViewDto
                            {
                                ProductId = fsi.Product.ProductId,
                                ProductName = fsi.Product.ProductName,
                                imageUrl = $"{baseUrl}{_config["ImageSettings:ProductPath"]}{fsi.Product.ProductImages.Where(pi => pi.ImageMain).Select(pi => pi.UrlProductImage).FirstOrDefault()}",
                                OriginalPrice = fsi.Product.OriginalPrice,
                            },
                            Quantity = fsi.Quantity,
                            SellPrice = fsi.SellPrice
                        }).ToList()
                    })
                    .ToListAsync();

                if (!flashSalesToday.Any() && !flashSalesTomorrow.Any())
                    return (false, "No flash sales found for today or tomorrow.", null);

                var result = new
                {
                    today = flashSalesToday,
                    tomorrow = flashSalesTomorrow
                };

                return (true, "Success", result);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> CreateFlashSaleAsync(FlashSaleDto dto)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var flashSaleExists = _context.FlashSales
                    .Any(fs => fs.DateSale == dto.Date && dto.StartTime >= fs.StartTime && dto.StartTime < fs.EndTime);
                if (flashSaleExists)
                    return (false, "A flash sale already exists for the specified date and time range.");

                if (dto.StartTime >= dto.EndTime)
                {
                    return (false, "End time must be after start time.");
                }

                if (dto.Items == null || dto.Items.Count == 0)
                {
                    return (false, "Flash sale must have at least one item.");
                }

                var flashSale = new FlashSale
                {
                    FlashSaleName = dto.FlashSaleName,
                    Description = dto.Description,
                    DateSale = dto.Date,
                    StartTime = dto.StartTime,
                    EndTime = dto.EndTime
                };
                _context.FlashSales.Add(flashSale);
                _context.SaveChanges();

                foreach (var item in dto.Items)
                {
                    if (item.Quantity <= 0 || item.SellPrice <= 0)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return (false, "Quantity and Sell Price must be greater than zero.");
                    }

                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return (false, $"Product with ID {item.ProductId} does not exist.");
                    }

                    var flashSaleItem = new FlashSaleItem
                    {
                        FlashSaleId = flashSale.FlashSaleId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        SellPrice = item.SellPrice
                    };
                    _context.FlashSaleItems.Add(flashSaleItem);
                    _context.SaveChanges();
                }

                await _unitOfWork.CommitTransactionAsync();
                return (true, "Create Success");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return (false, $"Internal server error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> AddFlashSaleItemAsync(FlashSaleItemAddDto dto)
        {
            try
            {
                var flashSale = _context.FlashSales.FirstOrDefault(f => f.FlashSaleId == dto.FlashSaleId);
                if (flashSale == null)
                {
                    return (false, "Flash sale not found.");
                }

                var product = await _context.Products.FindAsync(dto.ProductId);
                if (product == null)
                {
                    return (false, $"Product with ID {dto.ProductId} does not exist.");
                }

                var existingItem = _context.FlashSaleItems
                    .FirstOrDefault(fsi => fsi.FlashSaleId == dto.FlashSaleId && fsi.ProductId == dto.ProductId);
                if (existingItem != null)
                {
                    return (false, "This product is already added to the flash sale.");
                }

                var flashSaleItem = new FlashSaleItem
                {
                    FlashSaleId = dto.FlashSaleId,
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity,
                    SellPrice = dto.SellPrice
                };
                _context.FlashSaleItems.Add(flashSaleItem);
                _context.SaveChanges();

                return (true, "Create Success");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateFlashSaleAsync(int id, FlashSaleEditDto dto)
        {
            try
            {
                var flashSale = _context.FlashSales.FirstOrDefault(f => f.FlashSaleId == id);
                if (flashSale == null)
                {
                    return (false, "Flash sale not found.");
                }

                var flashSaleExists = _context.FlashSales
                    .Any(fs => fs.DateSale == dto.Date && dto.StartTime >= fs.StartTime && dto.StartTime < fs.EndTime && fs.FlashSaleId != flashSale.FlashSaleId);
                if (flashSaleExists)
                    return (false, "A flash sale already exists for the specified date and time range.");

                if (dto.StartTime >= dto.EndTime)
                {
                    return (false, "End time must be after start time.");
                }

                flashSale.FlashSaleName = dto.FlashSaleName;
                flashSale.Description = dto.Description;
                flashSale.DateSale = dto.Date;
                flashSale.StartTime = dto.StartTime;
                flashSale.EndTime = dto.EndTime;

                _context.Update(flashSale);
                _context.SaveChanges();

                return (true, "Update Success");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateFlashSaleItemAsync(int id, FlashSaleItemDto dto)
        {
            try
            {
                var flashSaleItem = _context.FlashSaleItems.FirstOrDefault(f => f.ItemId == id);
                if (flashSaleItem == null)
                {
                    return (false, "Flash sale item not found.");
                }

                flashSaleItem.Quantity = dto.Quantity;
                flashSaleItem.SellPrice = dto.SellPrice;

                _context.FlashSaleItems.Update(flashSaleItem);
                _context.SaveChanges();

                return (true, "Update Success");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteFlashSaleAsync(int id)
        {
            try
            {
                var flashSale = _context.FlashSales.FirstOrDefault(f => f.FlashSaleId == id);
                if (flashSale == null)
                {
                    return (false, "Flash sale not found.");
                }

                var flashSaleItems = _context.FlashSaleItems.Where(f => f.FlashSaleId == id).ToList();
                _context.FlashSaleItems.RemoveRange(flashSaleItems);
                _context.FlashSales.Remove(flashSale);
                _context.SaveChanges();

                return (true, "Delete Success");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteFlashSaleItemAsync(int id)
        {
            try
            {
                var flashSaleItem = _context.FlashSaleItems.FirstOrDefault(f => f.ItemId == id);
                if (flashSaleItem == null)
                {
                    return (false, "Flash sale item not found.");
                }

                _context.FlashSaleItems.Remove(flashSaleItem);
                _context.SaveChanges();

                return (true, "Delete Success");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message, object? Data)> GetFlashSalePriceAsync(int productId, int quantity)
        {
            try
            {
                var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == productId);
                if (product == null)
                    return (false, "Product not found", null);

                var today = DateOnly.FromDateTime(DateTime.Now);
                var now = TimeOnly.FromDateTime(DateTime.Now);

                var flashSaleItem = await _context.FlashSaleItems
                    .Include(fsi => fsi.FlashSale)
                    .FirstOrDefaultAsync(fsi => fsi.ProductId == productId &&
                        fsi.FlashSale.DateSale == today &&
                        fsi.FlashSale.StartTime <= now &&
                        fsi.FlashSale.EndTime > now &&
                        fsi.Quantity >= quantity);

                if (flashSaleItem == null)
                {
                    return (true, "Success", product.SellPrice);
                }

                return (true, "Success", flashSaleItem.SellPrice);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }
    }
}
