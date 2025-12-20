using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Helper;
using Google.Cloud.AIPlatform.V1;
using Google.Type;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FlashSaleController : ControllerBase
    {
        private readonly ElectronicStoreContext _context;
        private readonly IConfiguration _config;

        public FlashSaleController(ElectronicStoreContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }
        private string GetBaseUrl() => _config["AppSettings:BaseUrl"];
        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll()
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
                return Ok(flashSales);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFlashSaleById(int id)
        {
            try
            {
                var baseUrl = GetBaseUrl();

                var flashSale = await _context.FlashSales
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
                    .FirstOrDefaultAsync(fs => fs.FlashSaleId ==id);
                if (flashSale == null)
                    return NotFound("Flash sale not found.");
                return Ok(flashSale);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpGet("get-flashsale-today-and-tomorrow")]
        public async Task<IActionResult> GetFlashSaleTodayAndTomorrow()
        {
            try
            {
                var baseUrl = GetBaseUrl();

                var today = DateOnly.FromDateTime(System.DateTime.Now);
                var tomorrow = today.AddDays(1);
                var now = TimeOnly.FromDateTime(System.DateTime.Now);

                // Lấy flash sale hôm nay
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

                // Lấy flash sale ngày mai
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
                    return NotFound("No flash sales found for today or tomorrow.");

                // Kết hợp hai nhóm trong một object
                var result = new
                {
                    today = flashSalesToday,
                    tomorrow = flashSalesTomorrow
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpPost]
        public async Task<IActionResult> CreateFlashSale([FromBody] FlashSaleDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                var flashSaleExists = _context.FlashSales
                    .Any(fs => fs.DateSale == dto.Date && dto.StartTime >= fs.StartTime && dto.StartTime < fs.EndTime);
                if (flashSaleExists)
                    return BadRequest("A flash sale already exists for the specified date and time range.");
                if (dto.StartTime >= dto.EndTime)
                {
                    return BadRequest("End time must be after start time.");
                }
                if (dto.Items == null || dto.Items.Count == 0)
                {
                    return BadRequest("Flash sale must have at least one item.");
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
                        transaction.Rollback();
                        return BadRequest("Quantity and Sell Price must be greater than zero.");
                    }
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null)
                    {
                        transaction.Rollback();
                        return BadRequest($"Product with ID {item.ProductId} does not exist.");
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
                await transaction.CommitAsync();
                return Ok("Create Success");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [Authorize(Roles = "Admin,Employee")]

        [HttpPost("add-flashsaleItem")]
        public async Task<IActionResult> AddFlashSaleItem( [FromBody] FlashSaleItemAddDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                var flashSale = _context.FlashSales.FirstOrDefault(f => f.FlashSaleId == dto.FlashSaleId);
                if (flashSale == null)
                {
                    return NotFound("Flash sale not found.");
                }
                var product = await _context.Products.FindAsync(dto.ProductId);
                if (product == null)
                {
                    return BadRequest($"Product with ID {dto.ProductId} does not exist.");
                }
                var existingItem = _context.FlashSaleItems
                    .FirstOrDefault(fsi => fsi.FlashSaleId == dto.FlashSaleId && fsi.ProductId == dto.ProductId);
                if(existingItem != null)
                {
                    return BadRequest("This product is already added to the flash sale.");
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
                
                return Ok("Create Success");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [Authorize(Roles = "Admin,Employee")]

        [HttpPut]
        public async Task<IActionResult> Edit(int id, [FromBody] FlashSaleEditDto dto)
        {
            try
            {
                var flashSale = _context.FlashSales.FirstOrDefault(f => f.FlashSaleId == id);
                if(flashSale == null)
                {
                    return NotFound("Flash sale not found.");
                }
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                var flashSaleExists = _context.FlashSales
                    .Any(fs => fs.DateSale == dto.Date && dto.StartTime >= fs.StartTime && dto.StartTime < fs.EndTime && fs.FlashSaleId != flashSale.FlashSaleId);
                if (flashSaleExists)
                    return BadRequest("A flash sale already exists for the specified date and time range.");
                if (dto.StartTime >= dto.EndTime)
                {
                    return BadRequest("End time must be after start time.");
                }
                
                flashSale.FlashSaleName = dto.FlashSaleName;
                flashSale.Description = dto.Description;
                flashSale.DateSale = dto.Date;
                flashSale.StartTime = dto.StartTime;
                flashSale.EndTime = dto.EndTime;
               
                _context.Update(flashSale);
                _context.SaveChanges();
                
                return Ok("Update Success");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [Authorize(Roles = "Admin,Employee")]

        [HttpPut("edit-flashsale-item")]
        public async Task<IActionResult> EditFlashSaleItem(int id, [FromBody] FlashSaleItemDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                var flashSaleItem = _context.FlashSaleItems.FirstOrDefault(f => f.ItemId == id);
                if (flashSaleItem == null)
                {
                    return NotFound("Flash sale item not found.");
                }
                flashSaleItem.Quantity = dto.Quantity;
                flashSaleItem.SellPrice = dto.SellPrice;
                
                _context.FlashSaleItems.Update(flashSaleItem);
                _context.SaveChanges();

                
                return Ok("Create Success");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var flashSale = _context.FlashSales.FirstOrDefault(f => f.FlashSaleId == id);
                if (flashSale == null)
                {
                    return NotFound("Flash sale not found.");
                }
                var flashSaleItems = _context.FlashSaleItems.Where(f => f.FlashSaleId == id).ToList();
                _context.FlashSaleItems.RemoveRange(flashSaleItems);
                _context.FlashSales.Remove(flashSale);
                _context.SaveChanges();
                return Ok("Delete Success");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("delete-flashsale-item")]
        public async Task<IActionResult> DeleteFlashSaleItem(int id)
        {
            try
            {
                var flashSaleItem = _context.FlashSaleItems.FirstOrDefault(f => f.ItemId == id);
                if (flashSaleItem == null)
                {
                    return NotFound("Flash sale item not found.");
                }
                _context.FlashSaleItems.Remove(flashSaleItem);
                _context.SaveChanges();
                return Ok("Delete Success");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpGet("get-price-flashsale")]
        public async Task<IActionResult> UpdatePrice(int productId,int quantity)
        {
            try
            {
                var product =  await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == productId);
                if (product == null) return NotFound();
                var today = DateOnly.FromDateTime(System.DateTime.Now);
                var now = TimeOnly.FromDateTime(System.DateTime.Now);
                var flashSaleItem = await _context.FlashSaleItems
                    .Include(fsi => fsi.FlashSale)
                    .FirstOrDefaultAsync(fsi => fsi.ProductId == productId &&
                        fsi.FlashSale.DateSale == today &&
                        fsi.FlashSale.StartTime <= now &&
                        fsi.FlashSale.EndTime > now &&
                        fsi.Quantity >= quantity) 
                        ;
                if (flashSaleItem == null)
                {
                    return Ok(product.SellPrice);
                }
                return Ok(flashSaleItem.SellPrice);
                
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

    }
}
