using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Helper;
using global::ElectronicStore.Api.Data;
using global::ElectronicStore.Api.Dto;
using global::ElectronicStore.Api.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace ElectronicStore.Api.Controllers
{


    namespace ElectronicStore.Api.Controllers
    {
        [Route("api/[controller]")]
        [ApiController]
        public class CustomersController : ControllerBase
        {
            private readonly ElectronicStoreContext _context;
            private readonly IWebHostEnvironment _env;
            private readonly IConfiguration _config;

            public CustomersController(ElectronicStoreContext context, IWebHostEnvironment env, IConfiguration config)
            {
                _context = context;
                _env = env;
                _config = config;
            }
            private string GetFolder()
            {
                var relative = _config["AccountPath:AccountPath"] ?? "Image/AvatarAccount/";
                return Path.Combine(_env.WebRootPath ?? "wwwroot", relative);
            }

            private object MapCustomerToDto(Customer c)
            {
                try
                {
                    var baseUrl = _config["AppSettings:BaseUrl"];
                    return new
                    {
                        c.CustomerId,
                        c.FullName,
                        c.Address,
                        c.Gender,
                        c.BirthDate,
                        c.Phone,
                        c.Point,
                        Email = c.Account?.Email,
                        IsActive = c.Account?.IsActive,
                        ImageUrl = c.Account != null
                ? $"{baseUrl}{_config["ImageSettings:AccountPath"]}{c.Account.Avatar}"
                : null
                    };
                }
                catch (Exception ex)
                {
                    return StatusCode(500, "Internal server error: " + ex.Message);
                }

            }
            [HttpGet]
            [Authorize(Roles = "Admin,Employee")]
            public async Task<IActionResult> GetAll(int pageNumber = 1, int pageSize = 10)
            {
                try
                {
                    var query = _context.Customers
                    .Include(c => c.Account)
                    .OrderByDescending(c => c.CustomerId);

                    var totalItems = await query.CountAsync();

                    var customers = await query
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();
                    var result = customers.Select(MapCustomerToDto);
                    return Ok(new
                    {
                        TotalItems = totalItems,
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                        Data = result
                    });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, "Internal server error: " + ex.Message);
                }
            }

            // GET: api/customers/{id}
            [HttpGet("{id}")]
            [Authorize]
            public async Task<IActionResult> GetById(int id)
            {
                try
                {
                    var baseUrl = _config["AppSettings:BaseUrl"];

                    // Lấy thông tin customer kèm account (nếu có)
                    var customer = await _context.Customers
                        .Include(c => c.Account)
                        .Where(c => c.CustomerId == id)
                        .Select(c => new
                        {
                            c.CustomerId,
                            c.FullName,
                            c.Address,
                            c.Gender,
                            BirthDate = c.BirthDate.HasValue
                                ? c.BirthDate.Value.ToString("dd/MM/yyyy")
                                : null,
                            c.Phone,

                            // Nếu Account null → trả về null
                            Email = c.Account != null ? c.Account.Email : null,
                            IsActive = c.Account != null ? (bool?)c.Account.IsActive : null,

                            c.Point,

                            ImageUrl = c.Account != null
                                ? $"{baseUrl}{_config["ImageSettings:AccountPath"]}{c.Account.Avatar}"
                                : null
                        })
                        .FirstOrDefaultAsync();

                    if (customer == null)
                        return NotFound("Customer not found.");

                    // Kiểm tra quyền khách hàng (Customer role)
                    if (User.IsInRole("Customer"))
                    {
                        var accountId = int.Parse(User.FindFirst("AccountID").Value);

                        var customerOfUser = await _context.Customers
                            .FirstOrDefaultAsync(c => c.AccountId == accountId);

                        if (customerOfUser == null || customerOfUser.CustomerId != id)
                        {
                            return Forbid("You are not authorized to access this customer's information.");
                        }
                    }

                    return Ok(customer);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, "Internal server error: " + ex.Message);
                }
            }





            // GET: api/customers/search?phone=0123456789
            [HttpGet("search")]
            [Authorize(Roles = "Admin,Employee")]
            public async Task<IActionResult> SearchByPhone(string phone)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(phone))
                        return BadRequest("Phone number is required.");

                    var customers = await _context.Customers
                        .Include(c => c.Account)
                        .ThenInclude(a => a.Role)
                        .Where(c => c.Phone.Contains(phone))
                        .ToListAsync();

                    if (!customers.Any()) return NotFound("No customers found with this phone number.");

                    return Ok(customers.Select(MapCustomerToDto));
                }
                catch (Exception ex)
                {
                    return StatusCode(500, "Internal server error: " + ex.Message);
                }
            }

            // PUT: api/customers/{id}
            [HttpPut("{id}")]
            [Authorize(Roles = "Admin,Employee")]
            public async Task<IActionResult> Update(int id, [FromForm] CustomerDto dto)
            {
                try
                {
                    if (!ModelState.IsValid) return BadRequest(ModelState);

                    var customer = await _context.Customers.FindAsync(id);
                    if (customer == null) return NotFound("Customer not found.");
                    else
                    {
                        customer.FullName = dto.FullName;
                        customer.Address = dto.Address;
                        customer.Phone = dto.PhoneNumber;
                        _context.Customers.Update(customer);

                    }
                    var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == customer.AccountId);
                    if (account != null)
                    {
                        if (_context.Accounts.Any(a => a.Email == dto.Email && a.AccountId != account.AccountId))
                            return BadRequest("Email already exists");
                        if (_context.Customers.Any(a => a.Phone == dto.PhoneNumber && a.CustomerId != customer.CustomerId))
                            return BadRequest("Phone number already exists");

                        account.Email = dto.Email;
                        account.IsActive = dto.IsActive;
                        _context.Accounts.Update(account);

                    }



                    await _context.SaveChangesAsync();

                    return Ok("Customer updated successfully.");
                }
                catch (Exception ex)
                {
                    return StatusCode(500, "Internal server error: " + ex.Message);
                }
            }
            [HttpPut]
            [Route("EditMyProfile")]
            [Authorize(Roles = "Customer")]
            public async Task<IActionResult> Update([FromForm] CustomerProfileDto dto)
            {
                try
                {
                    if (!ModelState.IsValid) return BadRequest(ModelState);

                    

                    var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == int.Parse(User.FindFirst("AccountID").Value));
                    if (account == null) return NotFound("Account not found.");
                    var customer = await _context.Customers.FirstOrDefaultAsync( C => C.AccountId == account.AccountId);
                    if (customer == null) return NotFound("Customer not found.");
                    if (_context.Accounts.Any(a => a.Email == dto.Email && a.AccountId != account.AccountId))
                        return BadRequest("Email already exists");
                    if (_context.Customers.Any(a => a.Phone == dto.PhoneNumber && a.CustomerId != customer.CustomerId))
                        return BadRequest("Phone number already exists");
                    customer.FullName = dto.FullName;
                    customer.Phone = dto.PhoneNumber;
                    customer.Address = dto.Address;
                    customer.BirthDate = dto.BirthDate;
                    customer.Gender = dto.Gender;
                    account.Email = dto.Email;

                    if (dto.Avatar != null)
                    {
                        if (!ImageHelper.IsImageFile(dto.Avatar))
                            return BadRequest("Please upload a valid image file.");

                        var folder = GetFolder();
                        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                        // Xóa avatar cũ
                        if (!string.IsNullOrEmpty(account.Avatar) && account.Avatar != "default-avatar.jpg")

                        {
                            var oldPath = Path.Combine(folder, account.Avatar);
                            ImageHelper.DeleteFileIfExists(oldPath, account.Avatar);
                        }

                        // Lưu avatar mới
                        var ext = Path.GetExtension(dto.Avatar.FileName);
                        var avatarFile = $"{Guid.NewGuid().ToString()}{ext}";
                        var fullPath = Path.Combine(folder, avatarFile);

                        using (var fs = new FileStream(fullPath, FileMode.Create))
                        {
                            await dto.Avatar.CopyToAsync(fs);
                        }

                        account.Avatar = avatarFile;
                    }

                    _context.Customers.Update(customer);
                    await _context.SaveChangesAsync();

                    return Ok("Customer updated successfully.");
                }
                catch (Exception ex)
                {
                    return StatusCode(500, "Internal server error: " + ex.Message);
                }
            }


        }
    }

}
