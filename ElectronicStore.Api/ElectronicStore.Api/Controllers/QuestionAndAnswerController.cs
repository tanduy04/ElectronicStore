using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionAndAnswerController : ControllerBase
    {
        private readonly IQuestionAndAnswerService _qnaService;

        public QuestionAndAnswerController(IQuestionAndAnswerService qnaService)
        {
            _qnaService = qnaService;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _qnaService.GetAllAsync();
            if (!result.Success)
                return StatusCode(500, result.Message);
            return Ok(result.Data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _qnaService.GetByIdAsync(id);
            if (!result.Success)
                return NotFound(result.Message);
            return Ok(result.Data);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Create([FromBody] QuestionAndAnswerDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _qnaService.CreateAsync(dto);
            if (!result.Success)
                return BadRequest(result.Message);
            return Ok(result.Message);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Update(int id, [FromBody] QuestionAndAnswerDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _qnaService.UpdateAsync(id, dto);
            if (!result.Success)
                return NotFound(result.Message);
            return Ok(result.Message);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _qnaService.DeleteAsync(id);
            if (!result.Success)
                return NotFound(result.Message);
            return Ok(result.Message);
        }
    }
}
