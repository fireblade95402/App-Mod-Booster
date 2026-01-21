using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages.Expenses;

public class AddExpenseModel : PageModel
{
    private readonly DatabaseService _databaseService;

    public AddExpenseModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    [BindProperty]
    public CreateExpenseRequest ExpenseRequest { get; set; } = new CreateExpenseRequest
    {
        ExpenseDate = DateTime.Today,
        UserId = 1
    };

    public List<ExpenseCategory> Categories { get; set; } = new();
    public List<User> Users { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        Categories = await _databaseService.GetAllCategoriesAsync();
        Users = await _databaseService.GetAllUsersAsync();
    }

    public async Task<IActionResult> OnPostAsync(string action)
    {
        Categories = await _databaseService.GetAllCategoriesAsync();
        Users = await _databaseService.GetAllUsersAsync();

        if (!ModelState.IsValid)
        {
            ErrorMessage = "Please fill in all required fields.";
            return Page();
        }

        try
        {
            var expenseId = await _databaseService.CreateExpenseAsync(ExpenseRequest);

            if (action == "submit")
            {
                var submitted = await _databaseService.SubmitExpenseAsync(expenseId);
                if (submitted)
                {
                    SuccessMessage = "Expense submitted successfully!";
                }
                else
                {
                    ErrorMessage = "Expense created but failed to submit.";
                }
            }
            else
            {
                SuccessMessage = "Expense saved as draft!";
            }

            ExpenseRequest = new CreateExpenseRequest
            {
                ExpenseDate = DateTime.Today,
                UserId = ExpenseRequest.UserId
            };

            return Page();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error creating expense: {ex.Message}";
            return Page();
        }
    }
}
