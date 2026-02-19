using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class IndexModel : PageModel
{
    private readonly DatabaseService _dbService;
    private readonly ErrorHandlingService _errorService;
    private readonly ILogger<IndexModel> _logger;

    public List<Expense> Expenses { get; set; } = new();
    public List<ExpenseCategory> Categories { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public IndexModel(DatabaseService dbService, ErrorHandlingService errorService, ILogger<IndexModel> logger)
    {
        _dbService = dbService;
        _errorService = errorService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            Expenses = await _dbService.GetExpensesAsync();
            Categories = await _dbService.GetExpenseCategoriesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = _errorService.FormatDatabaseError(ex, "Index.cshtml.cs", "OnGetAsync");
            Expenses = _errorService.GetDummyExpenses();
            Categories = _errorService.GetDummyCategories();
            _logger.LogError(ex, "Error loading index page");
        }
    }
}
