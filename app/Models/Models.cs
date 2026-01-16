namespace ExpenseManagement.Models;

public class Expense
{
    public int ExpenseId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int AmountMinor { get; set; } // Amount in pence
    public string Currency { get; set; } = "GBP";
    public DateTime ExpenseDate { get; set; }
    public string? Description { get; set; }
    public string? ReceiptFile { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int? ReviewedBy { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Helper property to get amount in pounds
    public decimal AmountGBP => AmountMinor / 100.0m;
}

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

public class User
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int? ManagerId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ExpenseSummary
{
    public int TotalExpenses { get; set; }
    public int DraftCount { get; set; }
    public int SubmittedCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public int TotalAmountMinor { get; set; }
    public int ApprovedAmountMinor { get; set; }
    
    public decimal TotalAmountGBP => TotalAmountMinor / 100.0m;
    public decimal ApprovedAmountGBP => ApprovedAmountMinor / 100.0m;
}

public class CreateExpenseRequest
{
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public decimal AmountGBP { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string? Description { get; set; }
    public string? ReceiptFile { get; set; }
    public string StatusName { get; set; } = "Draft";
    
    public int AmountMinor => (int)(AmountGBP * 100);
}

public class UpdateExpenseRequest
{
    public int ExpenseId { get; set; }
    public int? CategoryId { get; set; }
    public decimal? AmountGBP { get; set; }
    public DateTime? ExpenseDate { get; set; }
    public string? Description { get; set; }
    public string? ReceiptFile { get; set; }
    
    public int? AmountMinor => AmountGBP.HasValue ? (int)(AmountGBP.Value * 100) : null;
}

public class ApproveRejectExpenseRequest
{
    public int ExpenseId { get; set; }
    public int ReviewedBy { get; set; }
}
