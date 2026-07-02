namespace SalaryManagementAPI.Models
{
    // A monthly spending limit set by the user for a given category
    public class Budget
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Category { get; set; } = string.Empty;
        public decimal LimitAmount { get; set; }
    }
}
