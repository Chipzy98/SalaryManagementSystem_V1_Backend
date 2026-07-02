using System.ComponentModel.DataAnnotations;

namespace SalaryManagementAPI.DTOs
{
    public class SalaryCreateDto
    {
        [Required]
        public DateTime Month { get; set; } // any date within the target month

        [Required, Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        [MaxLength(250)]
        public string? Note { get; set; }
    }

    public class SalaryResponseDto
    {
        public int Id { get; set; }
        public DateTime Month { get; set; }
        public decimal Amount { get; set; }
        public string? Note { get; set; }
    }
}
