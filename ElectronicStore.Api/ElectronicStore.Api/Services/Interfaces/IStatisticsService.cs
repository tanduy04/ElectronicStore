using ElectronicStore.Api.Dto;

namespace ElectronicStore.Api.Services.Interfaces
{
    public interface IStatisticsService
    {
        Task<(bool Success, string Message, object? Data)> GetStatisticsByDayAsync(DateTime? date = null);
        Task<(bool Success, string Message, object? Data)> GetStatisticsByMonthAsync(int? month = null, int? year = null);
        Task<(bool Success, string Message, object? Data)> GetStatisticsByYearAsync(int? year = null);
    }
}
