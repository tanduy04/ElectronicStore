using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Employee")]
    public class StatisticsController : ControllerBase
    {
        private readonly ElectronicStoreContext _context;

        public StatisticsController(ElectronicStoreContext context)
        {
            _context = context;
        }

        // =========================
        // 1️⃣ Thống kê theo ngày
        // =========================
        [HttpGet("by-day")]
        public async Task<IActionResult> GetByDay([FromQuery] DateTime? date)
        {
            DateTime targetDate = date?.Date ?? DateTime.UtcNow.Date;

            var totalRevenue = await _context.Orders
                .Where(o => o.Status == "Delivered" && o.OrderDate.Date == targetDate)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            var totalExpense = await _context.Imports
                .Where(i => i.Status == "Delivered" && i.ImportDate.Date == targetDate)
                .SumAsync(i => (decimal?)i.TotalAmount) ?? 0;

            var result = new StatisticsResultDto
            {
                Type = "Day",
                Period = targetDate.ToString("yyyy-MM-dd"),
                TotalRevenue = totalRevenue,
                TotalExpense = totalExpense,
                Profit = totalRevenue - totalExpense
            };

            return Ok(result);
        }

        // =========================
        // 2️⃣ Thống kê theo tháng
        // =========================
        [HttpGet("by-month")]
        public async Task<IActionResult> GetByMonth([FromQuery] int? month, [FromQuery] int? year)
        {
            int targetMonth = month ?? DateTime.UtcNow.Month;
            int targetYear = year ?? DateTime.UtcNow.Year;

            var totalRevenue = await _context.Orders
                .Where(o => o.Status == "Delivered" &&
                            o.OrderDate.Month == targetMonth &&
                            o.OrderDate.Year == targetYear)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            var totalExpense = await _context.Imports
                .Where(i => i.Status == "Delivered" &&
                            i.ImportDate.Month == targetMonth &&
                            i.ImportDate.Year == targetYear)
                .SumAsync(i => (decimal?)i.TotalAmount) ?? 0;

            var result = new StatisticsResultDto
            {
                Type = "Month",
                Period = $"{targetMonth}/{targetYear}",
                TotalRevenue = totalRevenue,
                TotalExpense = totalExpense,
                Profit = totalRevenue - totalExpense
            };

            return Ok(result);
        }

        // =========================
        // 3️⃣ Thống kê theo năm
        // =========================
        [HttpGet("by-year")]
        public async Task<IActionResult> GetByYear([FromQuery] int? year)
        {
            int targetYear = year ?? DateTime.UtcNow.Year;

            var totalRevenue = await _context.Orders
                .Where(o => o.Status == "Delivered" && o.OrderDate.Year == targetYear)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            var totalExpense = await _context.Imports
                .Where(i => i.Status == "Delivered" && i.ImportDate.Year == targetYear)
                .SumAsync(i => (decimal?)i.TotalAmount) ?? 0;

            var result = new StatisticsResultDto
            {
                Type = "Year",
                Period = targetYear.ToString(),
                TotalRevenue = totalRevenue,
                TotalExpense = totalExpense,
                Profit = totalRevenue - totalExpense
            };

            return Ok(result);
        }
    }
}
