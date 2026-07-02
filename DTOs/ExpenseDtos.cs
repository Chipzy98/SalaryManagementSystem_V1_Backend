using System.ComponentModel.DataAnnotations;

namespace SalaryManagementAPI.DTOs
{
    public class ExpenseCreateDto
    {
        [Required, Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required, MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Description { get; set; }
    }

    public class ExpenseResponseDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class BudgetCreateDto
    {
        [Required, MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required, Range(0, double.MaxValue)]
        public decimal LimitAmount { get; set; }
    }

    public class BudgetResponseDto
    {
        public int Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public decimal LimitAmount { get; set; }
        public decimal SpentThisMonth { get; set; }
        public bool IsOverBudget { get; set; }
    }
}
