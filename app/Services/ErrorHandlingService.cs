using ExpenseManagement.Models;

namespace ExpenseManagement.Services;

public class ErrorHandlingService
{
    private readonly ILogger<ErrorHandlingService> _logger;

    public ErrorHandlingService(ILogger<ErrorHandlingService> logger)
    {
        _logger = logger;
    }

    public string FormatDatabaseError(Exception ex, string fileName, string methodName)
    {
        _logger.LogError(ex, "Database error in {FileName}.{MethodName}", fileName, methodName);

        var errorMessage = $"Database Connection Error in {fileName}.{methodName}: ";

        if (ex.Message.Contains("managed identity", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase))
        {
            errorMessage += "Managed Identity authentication failed. ";
            errorMessage += "Fix: Ensure the App Service managed identity is assigned to the Azure SQL server with appropriate database roles (db_datareader, db_datawriter, EXECUTE). ";
            errorMessage += $"Details: {ex.Message}";
        }
        else if (ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
        {
            errorMessage += "Database connection timeout. ";
            errorMessage += "Fix: Check if the Azure SQL firewall rules allow access from this IP address and Azure services. ";
            errorMessage += $"Details: {ex.Message}";
        }
        else if (ex.Message.Contains("permission", StringComparison.OrdinalIgnoreCase) ||
                 ex.Message.Contains("denied", StringComparison.OrdinalIgnoreCase))
        {
            errorMessage += "Database permission error. ";
            errorMessage += "Fix: Grant the managed identity appropriate roles: db_datareader, db_datawriter, and EXECUTE permissions. ";
            errorMessage += $"Details: {ex.Message}";
        }
        else
        {
            errorMessage += $"Details: {ex.Message}";
        }

        return errorMessage;
    }

    public List<Expense> GetDummyExpenses()
    {
        return new List<Expense>
        {
            new Expense
            {
                ExpenseId = 1,
                UserId = 1,
                UserName = "Sample User",
                UserEmail = "sample@example.com",
                CategoryId = 1,
                CategoryName = "Travel",
                StatusId = 2,
                StatusName = "Submitted",
                AmountMinor = 2540,
                AmountGBP = 25.40m,
                Currency = "GBP",
                ExpenseDate = DateTime.Now.AddDays(-5),
                Description = "Taxi from airport (DUMMY DATA - Database connection failed)",
                SubmittedAt = DateTime.Now.AddDays(-5),
                CreatedAt = DateTime.Now.AddDays(-5)
            },
            new Expense
            {
                ExpenseId = 2,
                UserId = 1,
                UserName = "Sample User",
                UserEmail = "sample@example.com",
                CategoryId = 2,
                CategoryName = "Meals",
                StatusId = 3,
                StatusName = "Approved",
                AmountMinor = 1425,
                AmountGBP = 14.25m,
                Currency = "GBP",
                ExpenseDate = DateTime.Now.AddDays(-10),
                Description = "Client lunch (DUMMY DATA - Database connection failed)",
                SubmittedAt = DateTime.Now.AddDays(-10),
                ReviewedBy = 2,
                ReviewerName = "Sample Manager",
                ReviewedAt = DateTime.Now.AddDays(-9),
                CreatedAt = DateTime.Now.AddDays(-10)
            }
        };
    }

    public List<ExpenseCategory> GetDummyCategories()
    {
        return new List<ExpenseCategory>
        {
            new ExpenseCategory { CategoryId = 1, CategoryName = "Travel", IsActive = true },
            new ExpenseCategory { CategoryId = 2, CategoryName = "Meals", IsActive = true },
            new ExpenseCategory { CategoryId = 3, CategoryName = "Supplies", IsActive = true },
            new ExpenseCategory { CategoryId = 4, CategoryName = "Accommodation", IsActive = true },
            new ExpenseCategory { CategoryId = 5, CategoryName = "Other", IsActive = true }
        };
    }

    public List<User> GetDummyUsers()
    {
        return new List<User>
        {
            new User
            {
                UserId = 1,
                UserName = "Sample Employee",
                Email = "employee@example.com",
                RoleId = 1,
                RoleName = "Employee",
                IsActive = true,
                CreatedAt = DateTime.Now
            },
            new User
            {
                UserId = 2,
                UserName = "Sample Manager",
                Email = "manager@example.com",
                RoleId = 2,
                RoleName = "Manager",
                IsActive = true,
                CreatedAt = DateTime.Now
            }
        };
    }
}
