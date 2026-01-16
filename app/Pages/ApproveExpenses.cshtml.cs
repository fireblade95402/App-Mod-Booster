using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class ApproveExpensesModel : PageModel
{
    private readonly ExpenseService _expenseService;
    private readonly ILogger<ApproveExpensesModel> _logger;

    public List<Expense> PendingExpenses { get; set; } = new();
    public List<SelectListItem> Users { get; set; } = new();

    [BindProperty]
    public int? ManagerId { get; set; }

    public string? ErrorMessage { get; set; }
    public string? ErrorDetails { get; set; }
    public string? SuccessMessage { get; set; }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public ApproveExpensesModel(ExpenseService expenseService, ILogger<ApproveExpensesModel> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            // Load pending expenses
            PendingExpenses = await _expenseService.GetPendingExpensesAsync();

            // Load users (for manager selection)
            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading approve expenses page");
            ErrorMessage = "Error loading pending expenses";
            ErrorDetails = $"Error in {GetType().Name}: {ex.Message}";
            
            try
            {
                await LoadUsersAsync();
            }
            catch { }
        }
    }

    public async Task<IActionResult> OnPostApproveAsync(int expenseId, int managerId)
    {
        try
        {
            var request = new ApproveRejectExpenseRequest
            {
                ExpenseId = expenseId,
                ReviewedBy = managerId
            };

            var result = await _expenseService.ApproveExpenseAsync(request);
            
            if (result != null)
            {
                SuccessMessage = $"Expense #{expenseId} approved successfully!";
            }
            else
            {
                ErrorMessage = "Failed to approve expense";
                ErrorDetails = "The expense could not be approved. Please try again.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving expense {ExpenseId}", expenseId);
            ErrorMessage = "Error approving expense";
            ErrorDetails = $"Error in {GetType().Name}: {ex.Message}";
        }

        // Reload data
        await OnGetAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostRejectAsync(int expenseId, int managerId)
    {
        try
        {
            var request = new ApproveRejectExpenseRequest
            {
                ExpenseId = expenseId,
                ReviewedBy = managerId
            };

            var result = await _expenseService.RejectExpenseAsync(request);
            
            if (result != null)
            {
                SuccessMessage = $"Expense #{expenseId} rejected successfully!";
            }
            else
            {
                ErrorMessage = "Failed to reject expense";
                ErrorDetails = "The expense could not be rejected. Please try again.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting expense {ExpenseId}", expenseId);
            ErrorMessage = "Error rejecting expense";
            ErrorDetails = $"Error in {GetType().Name}: {ex.Message}";
        }

        // Reload data
        await OnGetAsync();
        return Page();
    }

    private async Task LoadUsersAsync()
    {
        var users = await _expenseService.GetUsersAsync();
        Users = users.Where(u => u.RoleName == "Manager")
            .Select(u => new SelectListItem(u.UserName, u.UserId.ToString()))
            .ToList();

        // Set default manager if not set
        if (!ManagerId.HasValue && Users.Any())
        {
            ManagerId = int.Parse(Users.First().Value);
        }
    }
}
