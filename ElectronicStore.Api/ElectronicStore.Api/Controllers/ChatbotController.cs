using ElectronicStore.Api.Models;
using ElectronicStore.Api.Services;
using ElectronicStore.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatbotController : ControllerBase
    {
        private readonly HybridRagChatbotService _chatbotService;
        private readonly ILogger<ChatbotController> _logger;
        private readonly IBrandService _brandService;
        private readonly ICategoryService _categoryService;

        public ChatbotController(
            HybridRagChatbotService chatbotService,
            ILogger<ChatbotController> logger,
            IBrandService brandService,
            ICategoryService categoryService)
        {
            _chatbotService = chatbotService;
            _logger = logger;
            _brandService = brandService;
            _categoryService = categoryService;
        }

        [HttpPost("send")]
        public async Task<ActionResult<HybridChatResponse>> AskQuestion([FromBody] ChatRequest request)
        {
            // Get brands and categories from services
            var brandsResult = await _brandService.GetAllBrandsAsync();
            var categoriesResult = await _categoryService.GetAllCategoriesAsync();

            if (!brandsResult.Success || !categoriesResult.Success)
            {
                return BadRequest("Unable to fetch required data");
            }

            // Cast the data appropriately
            var brands = brandsResult.Data as dynamic;
            var categories = categoriesResult.Data as dynamic;

            var response = await _chatbotService.ProcessQuestionAsync(
                request.Message,
                brands,
                categories);

            return Ok(response);
        }
    }
}
