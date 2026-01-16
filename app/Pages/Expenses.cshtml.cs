using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class ExpensesModel : PageModel
{
    private readonly ExpenseService _expenseService;
    private readonly ILogger<ExpensesModel> _logger;

    public List<Expense> Expenses { get; set; } = new();
    public List<SelectListItem> Categories { get; set; } = new();
    public List<SelectListItem> Statuses { get; set; } = new();
    public List<SelectListItem> Users { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? FilterUserId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? FilterCategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? FilterStatusId { get; set; }

    public string? ErrorMessage { get; set; }
    public string? ErrorDetails { get; set; }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public ExpensesModel(ExpenseService expenseService, ILogger<ExpensesModel> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            // Load expenses with filters
            Expenses = await _expenseService.GetExpensesAsync(FilterUserId, FilterStatusId, FilterCategoryId);

            // Load dropdown data
            await LoadDropdownsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading expenses page");
            ErrorMessage = "Error loading expenses data";
            ErrorDetails = $"Error in {GetType().Name}: {ex.Message}";
            
            // Try to load dropdowns even if expenses failed
            try
            {
                await LoadDropdownsAsync();
            }
            catch { }
        }
    }

    private async Task LoadDropdownsAsync()
    {
        var categories = await _expenseService.GetExpenseCategoriesAsync();
        Categories = new List<SelectListItem> { new SelectListItem("All Categories", "") }
            .Concat(categories.Select(c => new SelectListItem(c.CategoryName, c.CategoryId.ToString())))
            .ToList();

        var statuses = await _expenseService.GetExpenseStatusesAsync();
        Statuses = new List<SelectListItem> { new SelectListItem("All Statuses", "") }
            .Concat(statuses.Select(s => new SelectListItem(s.StatusName, s.StatusId.ToString())))
            .ToList();

        var users = await _expenseService.GetUsersAsync();
        Users = new List<SelectListItem> { new SelectListItem("All Users", "") }
            .Concat(users.Select(u => new SelectListItem(u.UserName, u.UserId.ToString())))
            .ToList();
    }
}
