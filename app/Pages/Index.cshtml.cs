using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class IndexModel : PageModel
{
    private readonly ExpenseService _expenseService;
    private readonly ILogger<IndexModel> _logger;

    public string? ErrorMessage { get; set; }
    public string? ErrorDetails { get; set; }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public IndexModel(ExpenseService expenseService, ILogger<IndexModel> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            // Try to connect to database to check if everything is working
            await _expenseService.GetExpenseCategoriesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection error on Index page");
            ErrorMessage = "Database Connection Error";
            ErrorDetails = GetDetailedErrorMessage(ex);
        }
    }

    private string GetDetailedErrorMessage(Exception ex)
    {
        var message = $"Error in {GetType().Name}: {ex.Message}";
        
        // Check for specific error types
        if (ex.Message.Contains("managed identity", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase))
        {
            message += "\n\nManaged Identity Configuration Issue:\n" +
                      "1. Ensure the App Service has a user-assigned managed identity assigned\n" +
                      "2. Verify the managed identity has been granted database access (db_datareader, db_datawriter, EXECUTE permissions)\n" +
                      "3. Check that the ManagedIdentityClientId app setting is set correctly\n" +
                      "4. Ensure the managed identity user exists in the database (run script.sql with the managed identity name)";
        }
        else if (ex.Message.Contains("login failed", StringComparison.OrdinalIgnoreCase))
        {
            message += "\n\nDatabase Access Issue:\n" +
                      "1. Verify the SQL Server firewall allows access from this App Service\n" +
                      "2. Check that the database exists and is named correctly (Northwind)\n" +
                      "3. Ensure Entra ID authentication is properly configured on the SQL Server";
        }
        else if (ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase) ||
                 ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
        {
            message += "\n\nNetwork Connectivity Issue:\n" +
                      "1. Check SQL Server firewall rules allow Azure services\n" +
                      "2. Verify the SQL Server FQDN is correct in configuration\n" +
                      "3. Ensure the App Service can reach the SQL Server endpoint";
        }
        
        return message;
    }
}
