using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class AddExpenseModel : PageModel
{
    private readonly ExpenseService _expenseService;
    private readonly ILogger<AddExpenseModel> _logger;

    [BindProperty]
    public CreateExpenseRequest Expense { get; set; } = new();

    public List<SelectListItem> Categories { get; set; } = new();
    public List<SelectListItem> Users { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public string? ErrorDetails { get; set; }
    public string? SuccessMessage { get; set; }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public AddExpenseModel(ExpenseService expenseService, ILogger<AddExpenseModel> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            await LoadDropdownsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading add expense page");
            ErrorMessage = "Error loading page data";
            ErrorDetails = $"Error in {GetType().Name}: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                ErrorMessage = "Validation Error";
                ErrorDetails = "Please fill in all required fields";
                return Page();
            }

            var createdExpense = await _expenseService.CreateExpenseAsync(Expense);
            
            if (createdExpense != null)
            {
                SuccessMessage = $"Expense created successfully! (ID: {createdExpense.ExpenseId})";
                // Reset form
                Expense = new CreateExpenseRequest { ExpenseDate = DateTime.Today, UserId = Expense.UserId };
                await LoadDropdownsAsync();
                return Page();
            }
            else
            {
                ErrorMessage = "Failed to create expense";
                ErrorDetails = "The expense was not created. Please try again.";
                await LoadDropdownsAsync();
                return Page();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            ErrorMessage = "Error creating expense";
            ErrorDetails = $"Error in {GetType().Name}: {ex.Message}";
            await LoadDropdownsAsync();
            return Page();
        }
    }

    private async Task LoadDropdownsAsync()
    {
        try
        {
            var categories = await _expenseService.GetExpenseCategoriesAsync();
            Categories = categories.Select(c => new SelectListItem
            {
                Value = c.CategoryId.ToString(),
                Text = c.CategoryName
            }).ToList();

            var users = await _expenseService.GetUsersAsync();
            Users = users.Select(u => new SelectListItem
            {
                Value = u.UserId.ToString(),
                Text = $"{u.UserName} ({u.RoleName})"
            }).ToList();

            // Set default values
            if (Expense.ExpenseDate == default)
            {
                Expense.ExpenseDate = DateTime.Today;
            }
            if (Expense.UserId == 0 && Users.Any())
            {
                Expense.UserId = int.Parse(Users.First().Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dropdowns");
            throw;
        }
    }
}
