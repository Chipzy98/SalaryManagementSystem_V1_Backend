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
    public class ExpenseController : ControllerBase
    {
        private readonly JsonDataStore _store;

        public ExpenseController(JsonDataStore store)
        {
            _store = store;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private static ExpenseResponseDto ToDto(Expense e) => new()
        {
            Id = e.Id,
            Amount = e.Amount,
            Date = e.Date,
            Category = e.Category,
            Description = e.Description
        };

        // GET /api/expense?month=2026-07-01&category=Food&search=coffee
        [HttpGet]
        public ActionResult<IEnumerable<ExpenseResponseDto>> GetAll(
            [FromQuery] DateTime? month,
            [FromQuery] string? category,
            [FromQuery] string? search,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var expenses = _store.Read(d =>
            {
                var query = d.Expenses.Where(e => e.UserId == CurrentUserId).AsEnumerable();

                if (month.HasValue)
                    query = query.Where(e => e.Date.Year == month.Value.Year && e.Date.Month == month.Value.Month);
                if (from.HasValue)
                    query = query.Where(e => e.Date >= from.Value.Date);
                if (to.HasValue)
                    query = query.Where(e => e.Date <= to.Value.Date);
                if (!string.IsNullOrWhiteSpace(category))
                    query = query.Where(e => e.Category == category);
                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(e => e.Description != null &&
                        e.Description.Contains(search, StringComparison.OrdinalIgnoreCase));

                return query.OrderByDescending(e => e.Date).Select(ToDto).ToList();
            });

            return Ok(expenses);
        }

        [HttpPost]
        public ActionResult<ExpenseResponseDto> Create(ExpenseCreateDto dto)
        {
            var expense = _store.Mutate(d =>
            {
                var e = new Expense
                {
                    Id = d.NextExpenseId++,
                    UserId = CurrentUserId,
                    Amount = dto.Amount,
                    Date = dto.Date.Date,
                    Category = dto.Category,
                    Description = dto.Description
                };
                d.Expenses.Add(e);
                return e;
            });

            return Ok(ToDto(expense));
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, ExpenseCreateDto dto)
        {
            var found = _store.Mutate(d =>
            {
                var expense = d.Expenses.FirstOrDefault(e => e.Id == id && e.UserId == CurrentUserId);
                if (expense == null) return false;

                expense.Amount = dto.Amount;
                expense.Date = dto.Date.Date;
                expense.Category = dto.Category;
                expense.Description = dto.Description;
                return true;
            });

            if (!found) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var found = _store.Mutate(d =>
            {
                var expense = d.Expenses.FirstOrDefault(e => e.Id == id && e.UserId == CurrentUserId);
                if (expense == null) return false;
                d.Expenses.Remove(expense);
                return true;
            });

            if (!found) return NotFound();
            return NoContent();
        }

        [HttpGet("categories")]
        public ActionResult<IEnumerable<string>> GetCategories()
        {
            var defaults = new[] { "Food", "Transport", "Shopping", "Bills", "Other" };

            var custom = _store.Read(d => d.Expenses
                .Where(e => e.UserId == CurrentUserId && !defaults.Contains(e.Category))
                .Select(e => e.Category)
                .Distinct()
                .ToList());

            return Ok(defaults.Concat(custom).Distinct());
        }
    }
}
