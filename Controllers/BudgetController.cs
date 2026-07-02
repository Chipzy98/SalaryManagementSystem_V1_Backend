using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalaryManagementAPI.Data;
using SalaryManagementAPI.DTOs;
using SalaryManagementAPI.Models;

namespace SalaryManagementAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BudgetController : ControllerBase
    {
        private readonly JsonDataStore _store;

        public BudgetController(JsonDataStore store)
        {
            _store = store;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public ActionResult<IEnumerable<BudgetResponseDto>> GetAll()
        {
            var now = DateTime.UtcNow;

            var result = _store.Read(d =>
            {
                var budgets = d.Budgets.Where(b => b.UserId == CurrentUserId).ToList();

                return budgets.Select(b =>
                {
                    var spent = d.Expenses
                        .Where(e => e.UserId == CurrentUserId
                            && e.Category == b.Category
                            && e.Date.Year == now.Year
                            && e.Date.Month == now.Month)
                        .Sum(e => e.Amount);

                    return new BudgetResponseDto
                    {
                        Id = b.Id,
                        Category = b.Category,
                        LimitAmount = b.LimitAmount,
                        SpentThisMonth = spent,
                        IsOverBudget = spent > b.LimitAmount
                    };
                }).ToList();
            });

            return Ok(result);
        }

        [HttpPost]
        public IActionResult Create(BudgetCreateDto dto)
        {
            _store.Mutate(d =>
            {
                var existing = d.Budgets.FirstOrDefault(b => b.UserId == CurrentUserId && b.Category == dto.Category);
                if (existing != null)
                {
                    existing.LimitAmount = dto.LimitAmount;
                }
                else
                {
                    d.Budgets.Add(new Budget
                    {
                        Id = d.NextBudgetId++,
                        UserId = CurrentUserId,
                        Category = dto.Category,
                        LimitAmount = dto.LimitAmount
                    });
                }
            });

            return Ok(new { message = "Budget saved." });
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var found = _store.Mutate(d =>
            {
                var budget = d.Budgets.FirstOrDefault(b => b.Id == id && b.UserId == CurrentUserId);
                if (budget == null) return false;
                d.Budgets.Remove(budget);
                return true;
            });

            if (!found) return NotFound();
            return NoContent();
        }
    }
}
