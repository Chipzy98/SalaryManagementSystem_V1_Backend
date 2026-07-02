namespace SalaryManagementAPI.Models
{
    public class Salary
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        // First day of the month this salary applies to, e.g. 2026-07-01
        public DateTime Month { get; set; }

        public decimal Amount { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
