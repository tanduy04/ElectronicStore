using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionAndAnswerController : ControllerBase
    {
        private readonly ElectronicStoreContext _context;

        public QuestionAndAnswerController(ElectronicStoreContext context)
        {
            _context = context;
        }

        // GET: api/QuestionAndAnswer/GetAll
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var qnas = await _context.QuestionAndAnswers
                    .OrderByDescending(q => q.Id)
                    .ToListAsync();
                return Ok(qnas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/QuestionAndAnswer/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var qna = await _context.QuestionAndAnswers.FindAsync(id);
                if (qna == null)
                    return NotFound("Question and Answer not found.");
                return Ok(qna);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // POST: api/QuestionAndAnswer
        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Create([FromBody] QuestionAndAnswerDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var qna = new QuestionAndAnswer
                {
                    Question = dto.Question.Trim(),
                    Answer = dto.Answer.Trim()
                };

                _context.QuestionAndAnswers.Add(qna);
                await _context.SaveChangesAsync();

                return Ok("Created Success");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // PUT: api/QuestionAndAnswer/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Update(int id, [FromBody] QuestionAndAnswerDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var qna = await _context.QuestionAndAnswers.FindAsync(id);
                if (qna == null)
                    return NotFound("Question and Answer not found.");

                qna.Question = dto.Question.Trim();
                qna.Answer = dto.Answer.Trim();

                _context.Update(qna);
                await _context.SaveChangesAsync();

                return Ok("Updated Success");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // DELETE: api/QuestionAndAnswer/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var qna = await _context.QuestionAndAnswers.FindAsync(id);
                if (qna == null)
                    return NotFound("Question and Answer not found.");

                _context.QuestionAndAnswers.Remove(qna);
                await _context.SaveChangesAsync();

                return Ok("Deleted Success");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
