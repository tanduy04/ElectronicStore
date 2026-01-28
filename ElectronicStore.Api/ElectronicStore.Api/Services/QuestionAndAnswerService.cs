using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Repositories.Interfaces;
using ElectronicStore.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStore.Api.Services
{
    public class QuestionAndAnswerService : IQuestionAndAnswerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ElectronicStoreContext _context;

        public QuestionAndAnswerService(IUnitOfWork unitOfWork, ElectronicStoreContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }

        public async Task<(bool Success, string Message, object? Data)> GetAllAsync()
        {
            try
            {
                var qnas = await _context.QuestionAndAnswers
                    .OrderByDescending(q => q.Id)
                    .ToListAsync();

                return (true, "Success", qnas);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> GetByIdAsync(int id)
        {
            try
            {
                var qna = await _context.QuestionAndAnswers.FindAsync(id);
                if (qna == null)
                    return (false, "Question and Answer not found.", null);

                return (true, "Success", qna);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> CreateAsync(QuestionAndAnswerDto dto)
        {
            try
            {
                var qna = new QuestionAndAnswer
                {
                    Question = dto.Question.Trim(),
                    Answer = dto.Answer.Trim()
                };

                _context.QuestionAndAnswers.Add(qna);
                await _context.SaveChangesAsync();

                return (true, "Created Success");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateAsync(int id, QuestionAndAnswerDto dto)
        {
            try
            {
                var qna = await _context.QuestionAndAnswers.FindAsync(id);
                if (qna == null)
                    return (false, "Question and Answer not found.");

                qna.Question = dto.Question.Trim();
                qna.Answer = dto.Answer.Trim();

                _context.Update(qna);
                await _context.SaveChangesAsync();

                return (true, "Updated Success");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            try
            {
                var qna = await _context.QuestionAndAnswers.FindAsync(id);
                if (qna == null)
                    return (false, "Question and Answer not found.");

                _context.QuestionAndAnswers.Remove(qna);
                await _context.SaveChangesAsync();

                return (true, "Deleted Success");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }
    }
}
