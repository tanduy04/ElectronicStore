using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Customer")]
    public class ProductController : ControllerBase
    {
        [HttpGet]
        [Route("GetAll")]

        public IActionResult GetProducts()
        {
            // This is a placeholder for actual product retrieval logic
            var products = new[]
            {
                new { Id = 1, Name = "Laptop", Price = 999.99 },
                new { Id = 2, Name = "Smartphone", Price = 499.99 },
                new { Id = 3, Name = "Tablet", Price = 299.99 }
            };

            return Ok(products);
        }
        
    }
}
