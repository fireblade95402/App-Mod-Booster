namespace ExpenseManagement.Models;

public class ExpenseCategory
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class ExpenseStatus
{
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
}

public class ExpenseSummary
{
    public string GroupName { get; set; } = string.Empty;
    public int ExpenseCount { get; set; }
    public int TotalAmountMinor { get; set; }
    public decimal TotalAmountGBP { get; set; }
}
