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
    public class SalaryController : ControllerBase
    {
        private readonly JsonDataStore _store;

        public SalaryController(JsonDataStore store)
        {
            _store = store;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private static DateTime FirstOfMonth(DateTime d) => new(d.Year, d.Month, 1);

        private static SalaryResponseDto ToDto(Salary s) =>
            new() { Id = s.Id, Month = s.Month, Amount = s.Amount, Note = s.Note };

        [HttpGet]
        public ActionResult<IEnumerable<SalaryResponseDto>> GetAll()
        {
            var salaries = _store.Read(d => d.Salaries
                .Where(s => s.UserId == CurrentUserId)
                .OrderByDescending(s => s.Month)
                .Select(ToDto)
                .ToList());

            return Ok(salaries);
        }

        [HttpPost]
        public ActionResult<SalaryResponseDto> Create(SalaryCreateDto dto)
        {
            var month = FirstOfMonth(dto.Month);

            var result = _store.Mutate(d =>
            {
                var existing = d.Salaries.FirstOrDefault(s => s.UserId == CurrentUserId && s.Month == month);
                if (existing != null)
                {
                    // Update existing entry for that month instead of duplicating
                    existing.Amount = dto.Amount;
                    existing.Note = dto.Note;
                    return existing;
                }

                var salary = new Salary
                {
                    Id = d.NextSalaryId++,
                    UserId = CurrentUserId,
                    Month = month,
                    Amount = dto.Amount,
                    Note = dto.Note
                };
                d.Salaries.Add(salary);
                return salary;
            });

            return Ok(ToDto(result));
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, SalaryCreateDto dto)
        {
            var found = _store.Mutate(d =>
            {
                var salary = d.Salaries.FirstOrDefault(s => s.Id == id && s.UserId == CurrentUserId);
                if (salary == null) return false;

                salary.Month = FirstOfMonth(dto.Month);
                salary.Amount = dto.Amount;
                salary.Note = dto.Note;
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
                var salary = d.Salaries.FirstOrDefault(s => s.Id == id && s.UserId == CurrentUserId);
                if (salary == null) return false;
                d.Salaries.Remove(salary);
                return true;
            });

            if (!found) return NotFound();
            return NoContent();
        }
    }
}
