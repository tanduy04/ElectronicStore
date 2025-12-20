using Microsoft.AspNetCore.Mvc;
using ElectronicStore.Api.Models;
using ElectronicStore.Api.Services;

namespace ElectronicStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly QdrantService _qdrantService;
        private readonly QADataService _qaDataService;
        private readonly ILogger<AdminController> _logger;
        private readonly IConfiguration _configuration;

        public AdminController(
            QdrantService qdrantService,
            QADataService qaDataService,
            ILogger<AdminController> logger,
            IConfiguration configuration)
        {
            _qdrantService = qdrantService;
            _qaDataService = qaDataService;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost("index")]
        public async Task<IActionResult> IndexData()
        {
            try
            {
                var filePath = _configuration["QAFilePath"] ?? "wwwroot/qa.txt";

                _logger.LogInformation("Starting indexing from file: {FilePath}", filePath);

                var documents = _qaDataService.ParseQAFile(filePath);
                
                _logger.LogInformation("Parsed {Count} QA pairs", documents.Count);

                await _qdrantService.InitializeCollectionAsync();
                var indexedCount = await _qdrantService.IndexQADocumentsAsync(documents);

                _logger.LogInformation("Successfully indexed {Count} documents", indexedCount);

                return Ok(new
                {
                    success = true,
                    totalDocuments = documents.Count,
                    indexedDocuments = indexedCount,
                    message = "Indexing completed successfully"
                });
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex, "QA file not found");
                return NotFound(new { error = "QA file not found", message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during indexing");
                return StatusCode(500, new { error = "Indexing failed", message = ex.Message });
            }
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearData()
        {
            try
            {
                _logger.LogInformation("Clearing collection");
                await _qdrantService.ClearCollectionAsync();

                return Ok(new
                {
                    success = true,
                    message = "Collection cleared successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing collection");
                return StatusCode(500, new { error = "Clear failed", message = ex.Message });
            }
        }

        [HttpPost("search")]
        public async Task<IActionResult> TestSearch([FromBody] ChatRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest(new { error = "Question cannot be empty" });
                }

                var contexts = await _qdrantService.SearchSimilarAsync(request.Message, topK: 5);

                return Ok(new
                {
                    query = request.Message,
                    foundContexts = contexts.Count,
                    contexts = contexts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during search");
                return StatusCode(500, new { error = "Search failed", message = ex.Message });
            }
        }
    }
}