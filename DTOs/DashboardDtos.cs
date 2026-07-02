namespace SalaryManagementAPI.DTOs
{
    public class CategoryBreakdownDto
    {
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public double Percentage { get; set; }
    }

    public class MonthlyOverviewDto
    {
        public DateTime Month { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal RemainingBalance { get; set; }
        public bool IsOverspending { get; set; }
        public List<CategoryBreakdownDto> CategoryBreakdown { get; set; } = new();
    }

    public class MonthComparisonPointDto
    {
        public DateTime Month { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal Savings { get; set; }
    }

    public class DashboardSummaryDto
    {
        public MonthlyOverviewDto CurrentMonth { get; set; } = new();
        public List<MonthComparisonPointDto> MonthlyTrend { get; set; } = new();
    }
}
