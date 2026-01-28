using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Repositories.Interfaces;
using ElectronicStore.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStore.Api.Services
{
    public class StatisticsService : IStatisticsService
    {
        private readonly IUnitOfWork _unitOfWork;

        public StatisticsService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<(bool Success, string Message, object? Data)> GetStatisticsByDayAsync(DateTime? date = null)
        {
            try
            {
                DateTime targetDate = date?.Date ?? DateTime.Now.Date;

                var orders = await _unitOfWork.Orders.GetAllAsync();
                var totalRevenue = orders
                    .Where(o => o.Status == "Delivered" && o.OrderDate.Date == targetDate)
                    .Sum(o => (decimal?)o.TotalAmount) ?? 0;

                var imports = await _unitOfWork.Imports.GetAllAsync();
                var totalExpense = imports
                    .Where(i => i.Status == "Delivered" && i.ImportDate.Date == targetDate)
                    .Sum(i => (decimal?)i.TotalAmount) ?? 0;

                var result = new StatisticsResultDto
                {
                    Type = "Day",
                    Period = targetDate.ToString("yyyy-MM-dd"),
                    TotalRevenue = totalRevenue,
                    TotalExpense = totalExpense,
                    Profit = totalRevenue - totalExpense
                };

                return (true, "Success", result);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> GetStatisticsByMonthAsync(int? month = null, int? year = null)
        {
            try
            {
                int targetMonth = month ?? DateTime.Now.Month;
                int targetYear = year ?? DateTime.Now.Year;

                var orders = await _unitOfWork.Orders.GetAllAsync();
                var totalRevenue = orders
                    .Where(o => o.Status == "Delivered" 
                        && o.OrderDate.Month == targetMonth 
                        && o.OrderDate.Year == targetYear)
                    .Sum(o => (decimal?)o.TotalAmount) ?? 0;

                var imports = await _unitOfWork.Imports.GetAllAsync();
                var totalExpense = imports
                    .Where(i => i.Status == "Delivered" 
                        && i.ImportDate.Month == targetMonth 
                        && i.ImportDate.Year == targetYear)
                    .Sum(i => (decimal?)i.TotalAmount) ?? 0;

                var result = new StatisticsResultDto
                {
                    Type = "Month",
                    Period = $"{targetMonth}/{targetYear}",
                    TotalRevenue = totalRevenue,
                    TotalExpense = totalExpense,
                    Profit = totalRevenue - totalExpense
                };

                return (true, "Success", result);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> GetStatisticsByYearAsync(int? year = null)
        {
            try
            {
                int targetYear = year ?? DateTime.Now.Year;

                var orders = await _unitOfWork.Orders.GetAllAsync();
                var totalRevenue = orders
                    .Where(o => o.Status == "Delivered" && o.OrderDate.Year == targetYear)
                    .Sum(o => (decimal?)o.TotalAmount) ?? 0;

                var imports = await _unitOfWork.Imports.GetAllAsync();
                var totalExpense = imports
                    .Where(i => i.Status == "Delivered" && i.ImportDate.Year == targetYear)
                    .Sum(i => (decimal?)i.TotalAmount) ?? 0;

                var result = new StatisticsResultDto
                {
                    Type = "Year",
                    Period = targetYear.ToString(),
                    TotalRevenue = totalRevenue,
                    TotalExpense = totalExpense,
                    Profit = totalRevenue - totalExpense
                };

                return (true, "Success", result);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }
    }
}
