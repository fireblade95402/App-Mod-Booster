namespace ExpenseManagement.Models;

public class Expense
{
    public int ExpenseId { get; set; }
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Receipt { get; set; }
    public int StatusId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public int? ApprovedBy { get; set; }
    public string? Comments { get; set; }
    
    public string? CategoryName { get; set; }
    public string? StatusName { get; set; }
    public string? UserName { get; set; }
    public string? ApproverName { get; set; }
}
