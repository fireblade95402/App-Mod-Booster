using Microsoft.Data.SqlClient;
using ExpenseManagement.Models;
using System.Data;

namespace ExpenseManagement.Services;

public class ExpenseService
{
    private readonly string _connectionString;
    private readonly ILogger<ExpenseService> _logger;

    public ExpenseService(IConfiguration configuration, ILogger<ExpenseService> logger)
    {
        _logger = logger;
        
        // Build connection string with managed identity
        var managedIdentityClientId = configuration["ManagedIdentityClientId"];
        var sqlServer = configuration["SqlServer"];
        var sqlDatabase = configuration["SqlDatabase"];
        
        if (!string.IsNullOrEmpty(sqlServer) && !string.IsNullOrEmpty(sqlDatabase) && !string.IsNullOrEmpty(managedIdentityClientId))
        {
            _connectionString = $"Server=tcp:{sqlServer};Database={sqlDatabase};Authentication=Active Directory Managed Identity;User Id={managedIdentityClientId};";
            _logger.LogInformation("Using Managed Identity connection for SQL Server: {Server}", sqlServer);
        }
        else
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string not found");
            _logger.LogInformation("Using default connection string for SQL Server");
        }
    }

    // Helper method to create and open connection
    private async Task<SqlConnection> GetConnectionAsync()
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }

    // Get all expense categories
    public async Task<List<ExpenseCategory>> GetExpenseCategoriesAsync()
    {
        var categories = new List<ExpenseCategory>();
        
        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand("dbo.GetExpenseCategories", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                categories.Add(new ExpenseCategory
                {
                    CategoryId = reader.GetInt32(0),
                    CategoryName = reader.GetString(1),
                    IsActive = reader.GetBoolean(2)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expense categories");
            throw;
        }
        
        return categories;
    }

    // Get all expense statuses
    public async Task<List<ExpenseStatus>> GetExpenseStatusesAsync()
    {
        var statuses = new List<ExpenseStatus>();
        
        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand("dbo.GetExpenseStatuses", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                statuses.Add(new ExpenseStatus
                {
                    StatusId = reader.GetInt32(0),
                    StatusName = reader.GetString(1)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expense statuses");
            throw;
        }
        
        return statuses;
    }

    // Get all users
    public async Task<List<User>> GetUsersAsync()
    {
        var users = new List<User>();
        
        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand("dbo.GetUsers", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    UserId = reader.GetInt32(0),
                    UserName = reader.GetString(1),
                    Email = reader.GetString(2),
                    RoleId = reader.GetInt32(3),
                    RoleName = reader.GetString(4),
                    ManagerId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    IsActive = reader.GetBoolean(6),
                    CreatedAt = reader.GetDateTime(7)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            throw;
        }
        
        return users;
    }

    // Get expenses with optional filters
    public async Task<List<Expense>> GetExpensesAsync(int? userId = null, int? statusId = null, int? categoryId = null)
    {
        var expenses = new List<Expense>();
        
        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand("dbo.GetExpenses", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@UserId", userId.HasValue ? userId.Value : DBNull.Value);
            command.Parameters.AddWithValue("@StatusId", statusId.HasValue ? statusId.Value : DBNull.Value);
            command.Parameters.AddWithValue("@CategoryId", categoryId.HasValue ? categoryId.Value : DBNull.Value);
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(ReadExpense(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expenses");
            throw;
        }
        
        return expenses;
    }

    // Get expense by ID
    public async Task<Expense?> GetExpenseByIdAsync(int expenseId)
    {
        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand("dbo.GetExpenseById", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return ReadExpense(reader);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expense by ID: {ExpenseId}", expenseId);
            throw;
        }
        
        return null;
    }

    // Get pending expenses for approval
    public async Task<List<Expense>> GetPendingExpensesAsync(int? managerId = null)
    {
        var expenses = new List<Expense>();
        
        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand("dbo.GetPendingExpenses", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@ManagerId", managerId.HasValue ? managerId.Value : DBNull.Value);
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(new Expense
                {
                    ExpenseId = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    UserName = reader.GetString(2),
                    CategoryId = reader.GetInt32(3),
                    CategoryName = reader.GetString(4),
                    StatusId = reader.GetInt32(5),
                    StatusName = reader.GetString(6),
                    AmountMinor = reader.GetInt32(7),
                    Currency = reader.GetString(8),
                    ExpenseDate = reader.GetDateTime(9),
                    Description = reader.IsDBNull(10) ? null : reader.GetString(10),
                    ReceiptFile = reader.IsDBNull(11) ? null : reader.GetString(11),
                    SubmittedAt = reader.IsDBNull(12) ? null : reader.GetDateTime(12),
                    CreatedAt = reader.GetDateTime(13)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending expenses");
            throw;
        }
        
        return expenses;
    }

    // Create new expense
    public async Task<Expense?> CreateExpenseAsync(CreateExpenseRequest request)
    {
        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand("dbo.CreateExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@UserId", request.UserId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@AmountMinor", request.AmountMinor);
            command.Parameters.AddWithValue("@Currency", "GBP");
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.AddWithValue("@Description", request.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ReceiptFile", request.ReceiptFile ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@StatusName", request.StatusName);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return ReadExpense(reader);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            throw;
        }
        
        return null;
    }

    // Update expense
    public async Task<Expense?> UpdateExpenseAsync(UpdateExpenseRequest request)
    {
        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand("dbo.UpdateExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@ExpenseId", request.ExpenseId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@AmountMinor", request.AmountMinor ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Description", request.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ReceiptFile", request.ReceiptFile ?? (object)DBNull.Value);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return ReadExpense(reader);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expense");
            throw;
        }
        
        return null;
    }

    // Submit expense
    public async Task<Expense?> SubmitExpenseAsync(int expenseId)
    {
        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand("dbo.SubmitExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return ReadExpense(reader);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting expense");
            throw;
        }
        
        return null;
    }

    // Approve expense
    public async Task<Expense?> ApproveExpenseAsync(ApproveRejectExpenseRequest request)
    {
        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand("dbo.ApproveExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@ExpenseId", request.ExpenseId);
            command.Parameters.AddWithValue("@ReviewedBy", request.ReviewedBy);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return ReadExpense(reader);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving expense");
            throw;
        }
        
        return null;
    }

    // Reject expense
    public async Task<Expense?> RejectExpenseAsync(ApproveRejectExpenseRequest request)
    {
        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand("dbo.RejectExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@ExpenseId", request.ExpenseId);
            command.Parameters.AddWithValue("@ReviewedBy", request.ReviewedBy);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return ReadExpense(reader);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting expense");
            throw;
        }
        
        return null;
    }

    // Delete expense
    public async Task<bool> DeleteExpenseAsync(int expenseId)
    {
        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand("dbo.DeleteExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            
            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expense");
            throw;
        }
    }

    // Get expense summary
    public async Task<ExpenseSummary?> GetExpenseSummaryAsync(int? userId = null)
    {
        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand("dbo.GetExpenseSummary", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@UserId", userId ?? (object)DBNull.Value);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new ExpenseSummary
                {
                    TotalExpenses = reader.GetInt32(0),
                    DraftCount = reader.GetInt32(1),
                    SubmittedCount = reader.GetInt32(2),
                    ApprovedCount = reader.GetInt32(3),
                    RejectedCount = reader.GetInt32(4),
                    TotalAmountMinor = reader.GetInt32(5),
                    ApprovedAmountMinor = reader.GetInt32(6)
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expense summary");
            throw;
        }
        
        return null;
    }

    // Helper method to read expense from data reader
    private Expense ReadExpense(SqlDataReader reader)
    {
        return new Expense
        {
            ExpenseId = reader.GetInt32(0),
            UserId = reader.GetInt32(1),
            UserName = reader.GetString(2),
            CategoryId = reader.GetInt32(3),
            CategoryName = reader.GetString(4),
            StatusId = reader.GetInt32(5),
            StatusName = reader.GetString(6),
            AmountMinor = reader.GetInt32(7),
            Currency = reader.GetString(8),
            ExpenseDate = reader.GetDateTime(9),
            Description = reader.IsDBNull(10) ? null : reader.GetString(10),
            ReceiptFile = reader.IsDBNull(11) ? null : reader.GetString(11),
            SubmittedAt = reader.IsDBNull(12) ? null : reader.GetDateTime(12),
            ReviewedBy = reader.IsDBNull(13) ? null : reader.GetInt32(13),
            ReviewedByName = reader.IsDBNull(14) ? null : reader.GetString(14),
            ReviewedAt = reader.IsDBNull(15) ? null : reader.GetDateTime(15),
            CreatedAt = reader.GetDateTime(16)
        };
    }
}
