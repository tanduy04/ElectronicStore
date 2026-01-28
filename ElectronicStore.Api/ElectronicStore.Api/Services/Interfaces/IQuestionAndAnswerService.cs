using ElectronicStore.Api.Dto;

namespace ElectronicStore.Api.Services.Interfaces
{
    public interface IQuestionAndAnswerService
    {
        Task<(bool Success, string Message, object? Data)> GetAllAsync();
        Task<(bool Success, string Message, object? Data)> GetByIdAsync(int id);
        Task<(bool Success, string Message)> CreateAsync(QuestionAndAnswerDto dto);
        Task<(bool Success, string Message)> UpdateAsync(int id, QuestionAndAnswerDto dto);
        Task<(bool Success, string Message)> DeleteAsync(int id);
    }
}
