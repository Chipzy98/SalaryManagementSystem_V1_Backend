using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalaryManagementAPI.Data;
using SalaryManagementAPI.DTOs;

namespace SalaryManagementAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly JsonDataStore _store;

        public DashboardController(JsonDataStore store)
        {
            _store = store;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET /api/dashboard/summary?month=2026-07-01&trendMonths=6
        [HttpGet("summary")]
        public ActionResult<DashboardSummaryDto> GetSummary([FromQuery] DateTime? month, [FromQuery] int trendMonths = 6)
        {
            var target = month.HasValue ? new DateTime(month.Value.Year, month.Value.Month, 1) : new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            var summary = _store.Read(d =>
            {
                var currentOverview = BuildMonthlyOverview(d, target);

                var trend = new List<MonthComparisonPointDto>();
                for (int i = trendMonths - 1; i >= 0; i--)
                {
                    var m = target.AddMonths(-i);
                    var income = d.Salaries.Where(s => s.UserId == CurrentUserId && s.Month == m).Sum(s => s.Amount);
                    var expenses = d.Expenses
                        .Where(e => e.UserId == CurrentUserId && e.Date.Year == m.Year && e.Date.Month == m.Month)
                        .Sum(e => e.Amount);

                    trend.Add(new MonthComparisonPointDto
                    {
                        Month = m,
                        TotalIncome = income,
                        TotalExpenses = expenses,
                        Savings = income - expenses
                    });
                }

                return new DashboardSummaryDto { CurrentMonth = currentOverview, MonthlyTrend = trend };
            });

            return Ok(summary);
        }

        [HttpGet("overview")]
        public ActionResult<MonthlyOverviewDto> GetOverview([FromQuery] DateTime? month)
        {
            var target = month.HasValue ? new DateTime(month.Value.Year, month.Value.Month, 1) : new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var overview = _store.Read(d => BuildMonthlyOverview(d, target));
            return Ok(overview);
        }

        private MonthlyOverviewDto BuildMonthlyOverview(DataFile d, DateTime month)
        {
            var totalIncome = d.Salaries
                .Where(s => s.UserId == CurrentUserId && s.Month == month)
                .Sum(s => s.Amount);

            var expenses = d.Expenses
                .Where(e => e.UserId == CurrentUserId && e.Date.Year == month.Year && e.Date.Month == month.Month)
                .ToList();

            var totalExpenses = expenses.Sum(e => e.Amount);

            var breakdown = expenses
                .GroupBy(e => e.Category)
                .Select(g => new CategoryBreakdownDto
                {
                    Category = g.Key,
                    Amount = g.Sum(e => e.Amount),
                    Percentage = totalExpenses > 0 ? (double)(g.Sum(e => e.Amount) / totalExpenses) * 100 : 0
                })
                .OrderByDescending(c => c.Amount)
                .ToList();

            return new MonthlyOverviewDto
            {
                Month = month,
                TotalIncome = totalIncome,
                TotalExpenses = totalExpenses,
                RemainingBalance = totalIncome - totalExpenses,
                IsOverspending = totalExpenses > totalIncome,
                CategoryBreakdown = breakdown
            };
        }
    }
}
