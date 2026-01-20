using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages.Expenses;

public class ApproveExpensesModel : PageModel
{
    private readonly DatabaseService _databaseService;

    public ApproveExpensesModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public List<Expense> PendingExpenses { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        PendingExpenses = await _databaseService.GetPendingExpensesAsync();
    }

    public async Task<IActionResult> OnPostAsync(int expenseId, string action)
    {
        const int managerId = 2;

        try
        {
            bool result;
            if (action == "approve")
            {
                result = await _databaseService.ApproveExpenseAsync(expenseId, managerId, "Approved");
                SuccessMessage = result ? "Expense approved successfully!" : "Failed to approve expense.";
            }
            else if (action == "reject")
            {
                result = await _databaseService.RejectExpenseAsync(expenseId, managerId, "Rejected");
                SuccessMessage = result ? "Expense rejected successfully!" : "Failed to reject expense.";
            }
            else
            {
                ErrorMessage = "Invalid action.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error processing expense: {ex.Message}";
        }

        PendingExpenses = await _databaseService.GetPendingExpensesAsync();
        return Page();
    }
}
