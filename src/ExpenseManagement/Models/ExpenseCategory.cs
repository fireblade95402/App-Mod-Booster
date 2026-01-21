namespace ExpenseManagement.Models;

public class ExpenseCategory
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
