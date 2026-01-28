using ElectronicStore.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Employee")]
    public class StatisticsController : ControllerBase
    {
        private readonly IStatisticsService _statisticsService;

        public StatisticsController(IStatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }

        [HttpGet("by-day")]
        public async Task<IActionResult> GetByDay([FromQuery] DateTime? date)
        {
            var result = await _statisticsService.GetStatisticsByDayAsync(date);
            if (!result.Success)
                return StatusCode(500, result.Message);
            return Ok(result.Data);
        }

        [HttpGet("by-month")]
        public async Task<IActionResult> GetByMonth([FromQuery] int? month, [FromQuery] int? year)
        {
            var result = await _statisticsService.GetStatisticsByMonthAsync(month, year);
            if (!result.Success)
                return StatusCode(500, result.Message);
            return Ok(result.Data);
        }

        [HttpGet("by-year")]
        public async Task<IActionResult> GetByYear([FromQuery] int? year)
        {
            var result = await _statisticsService.GetStatisticsByYearAsync(year);
            if (!result.Success)
                return StatusCode(500, result.Message);
            return Ok(result.Data);
        }
    }
}
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
