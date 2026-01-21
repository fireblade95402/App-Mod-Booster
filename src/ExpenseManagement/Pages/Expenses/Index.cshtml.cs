using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages.Expenses;

public class IndexModel : PageModel
{
    private readonly DatabaseService _databaseService;

    public IndexModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public List<Expense> Expenses { get; set; } = new();
    public List<Expense> FilteredExpenses { get; set; } = new();
    public List<ExpenseCategory> Categories { get; set; } = new();
    public List<ExpenseStatus> Statuses { get; set; } = new();
    
    public int? StatusFilter { get; set; }
    public int? CategoryFilter { get; set; }

    public async Task OnGetAsync(int? statusFilter, int? categoryFilter)
    {
        StatusFilter = statusFilter;
        CategoryFilter = categoryFilter;

        Expenses = await _databaseService.GetAllExpensesAsync();
        Categories = await _databaseService.GetAllCategoriesAsync();
        Statuses = await _databaseService.GetAllStatusesAsync();

        FilteredExpenses = Expenses;

        if (statusFilter.HasValue)
        {
            FilteredExpenses = FilteredExpenses.Where(e => e.StatusId == statusFilter.Value).ToList();
        }

        if (categoryFilter.HasValue)
        {
            FilteredExpenses = FilteredExpenses.Where(e => e.CategoryId == categoryFilter.Value).ToList();
        }
    }
}
