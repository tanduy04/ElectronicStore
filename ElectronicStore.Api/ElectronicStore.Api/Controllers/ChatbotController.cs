using ElectronicStore.Api.Data;
using ElectronicStore.Api.Models;
using ElectronicStore.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatbotController : ControllerBase
    {
        private readonly ElectronicStoreContext _dbContext;
        private readonly HybridRagChatbotService _chatbotService;
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(
            HybridRagChatbotService chatbotService,
            ILogger<ChatbotController> logger,
            ElectronicStoreContext dbContext) // THÊM
        {
            _chatbotService = chatbotService;
            _logger = logger;
            _dbContext = dbContext; // THÊM
        }

        [HttpPost("send")]
        public async Task<ActionResult<HybridChatResponse>> AskQuestion([FromBody] ChatRequest request)
        {
            // ... validation ...

            // Lấy brands và categories từ DB THỰC
            var brands =_dbContext.Brands
    .Where(b => b.IsActive)
    .ToList();

            var categories =  _dbContext.Categories
                .Where(c => c.IsActive)
                .ToList();

            var response = await _chatbotService.ProcessQuestionAsync(
                request.Message,
                brands,
                categories);

            return Ok(response);
        }
    }
}
