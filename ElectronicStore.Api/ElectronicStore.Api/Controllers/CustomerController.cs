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

            private string GetBaseUrl() => $"{Request.Scheme}://{Request.Host}/";

            private object MapCustomerToDto(Customer c)
            {
                return new
                {
                    c.CustomerId,
                    c.FullName,
                    c.Address,
                    c.Gender,
                    c.BirthDate,
                    c.Account.PhoneNumber,
                    c.Account.Email,
                    c.Account.IsActive,
                    c.Account.Avatar,
                };
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
            [Authorize(Roles = "Admin,Employee,Customer")]
            public async Task<IActionResult> GetById(int id)
            {
                try
                {
                    var customer = await _context.Customers
                    .Include(c => c.Account)
                    .Where(c => c.CustomerId == id)
                    .Select(c => new
                    {
                        c.CustomerId,
                        c.FullName,
                        c.Address,
                        c.Gender,
                        c.BirthDate,
                        c.Account.PhoneNumber,
                        c.Account.Email,
                        c.Point,
                        c.Account.IsActive,
                    })
                    .FirstOrDefaultAsync();

                    if (customer == null) return NotFound("Customer not found.");
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
                        .Where(c => c.Account.PhoneNumber == phone)
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
                    if (_context.Accounts.Any(a => a.Email == dto.Email))
                        return BadRequest("Email already exists");
                    if (_context.Accounts.Any(a => a.PhoneNumber == dto.PhoneNumber))
                        return BadRequest("Phone number already exists");
                    var customer = await _context.Customers.FindAsync(id);
                    if (customer == null) return NotFound("Customer not found.");
                    var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == customer.AccountId);
                    customer.FullName = dto.FullName;
                    account.Email = dto.Email;
                    account.PhoneNumber = dto.PhoneNumber;
                    customer.Address = dto.Address;

                    _context.Accounts.Update(account);
                    _context.Customers.Update(customer);
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
                    if (_context.Accounts.Any(a => a.Email == dto.Email))
                        return BadRequest("Email already exists");
                    if (_context.Accounts.Any(a => a.PhoneNumber == dto.PhoneNumber))
                        return BadRequest("Phone number already exists");
                    var customer = await _context.Customers.FindAsync(User.IsInRole);
                    if (customer == null) return NotFound("Customer not found.");
                    var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == customer.AccountId);
                    customer.FullName = dto.FullName;
                    account.Email = dto.Email;
                    account.PhoneNumber = dto.PhoneNumber;
                    customer.Address = dto.Address;
                    customer.BirthDate = dto.BirthDate;
                    customer.Gender = dto.Gender;
                    if (dto.Avatar != null)
                    {
                        if (!ImageHelper.IsImageFile(dto.Avatar))
                            return BadRequest("Please upload a valid image file.");

                        var folder = GetFolder();
                        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                        // Xóa avatar cũ
                        if (!string.IsNullOrEmpty(account.Avatar))
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
