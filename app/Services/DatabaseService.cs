using Microsoft.Data.SqlClient;
using ExpenseManagement.Models;

namespace ExpenseManagement.Services;

public class DatabaseService
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseService> _logger;

    public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _logger = logger;
    }

    private SqlConnection GetConnection()
    {
        return new SqlConnection(_connectionString);
    }

    // Users
    public async Task<List<User>> GetUsersAsync()
    {
        var users = new List<User>();
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            
            using var command = new SqlCommand("dbo.sp_GetUsers", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
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
                    ManagerName = reader.IsDBNull(6) ? null : reader.GetString(6),
                    IsActive = reader.GetBoolean(7),
                    CreatedAt = reader.GetDateTime(8)
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

    // Categories
    public async Task<List<ExpenseCategory>> GetExpenseCategoriesAsync()
    {
        var categories = new List<ExpenseCategory>();
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            
            using var command = new SqlCommand("dbo.sp_GetExpenseCategories", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
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

    // Statuses
    public async Task<List<ExpenseStatus>> GetExpenseStatusesAsync()
    {
        var statuses = new List<ExpenseStatus>();
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            
            using var command = new SqlCommand("dbo.sp_GetExpenseStatuses", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
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

    // Expenses
    public async Task<List<Expense>> GetExpensesAsync(string? statusName = null, int? userId = null)
    {
        var expenses = new List<Expense>();
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            
            using var command = new SqlCommand("dbo.sp_GetExpenses", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@StatusName", (object?)statusName ?? DBNull.Value);
            command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(new Expense
                {
                    ExpenseId = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    UserName = reader.GetString(2),
                    UserEmail = reader.GetString(3),
                    CategoryId = reader.GetInt32(4),
                    CategoryName = reader.GetString(5),
                    StatusId = reader.GetInt32(6),
                    StatusName = reader.GetString(7),
                    AmountMinor = reader.GetInt32(8),
                    AmountGBP = reader.GetDecimal(9),
                    Currency = reader.GetString(10),
                    ExpenseDate = reader.GetDateTime(11),
                    Description = reader.IsDBNull(12) ? null : reader.GetString(12),
                    ReceiptFile = reader.IsDBNull(13) ? null : reader.GetString(13),
                    SubmittedAt = reader.IsDBNull(14) ? null : reader.GetDateTime(14),
                    ReviewedBy = reader.IsDBNull(15) ? null : reader.GetInt32(15),
                    ReviewerName = reader.IsDBNull(16) ? null : reader.GetString(16),
                    ReviewedAt = reader.IsDBNull(17) ? null : reader.GetDateTime(17),
                    CreatedAt = reader.GetDateTime(18)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expenses");
            throw;
        }
        return expenses;
    }

    public async Task<Expense?> GetExpenseByIdAsync(int expenseId)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            
            using var command = new SqlCommand("dbo.sp_GetExpenseById", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Expense
                {
                    ExpenseId = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    UserName = reader.GetString(2),
                    UserEmail = reader.GetString(3),
                    CategoryId = reader.GetInt32(4),
                    CategoryName = reader.GetString(5),
                    StatusId = reader.GetInt32(6),
                    StatusName = reader.GetString(7),
                    AmountMinor = reader.GetInt32(8),
                    AmountGBP = reader.GetDecimal(9),
                    Currency = reader.GetString(10),
                    ExpenseDate = reader.GetDateTime(11),
                    Description = reader.IsDBNull(12) ? null : reader.GetString(12),
                    ReceiptFile = reader.IsDBNull(13) ? null : reader.GetString(13),
                    SubmittedAt = reader.IsDBNull(14) ? null : reader.GetDateTime(14),
                    ReviewedBy = reader.IsDBNull(15) ? null : reader.GetInt32(15),
                    ReviewerName = reader.IsDBNull(16) ? null : reader.GetString(16),
                    ReviewedAt = reader.IsDBNull(17) ? null : reader.GetDateTime(17),
                    CreatedAt = reader.GetDateTime(18)
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expense {ExpenseId}", expenseId);
            throw;
        }
        return null;
    }

    public async Task<int> CreateExpenseAsync(CreateExpenseRequest request)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            
            using var command = new SqlCommand("dbo.sp_CreateExpense", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@UserId", request.UserId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@AmountMinor", (int)(request.Amount * 100));
            command.Parameters.AddWithValue("@Currency", request.Currency);
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@ReceiptFile", (object?)request.ReceiptFile ?? DBNull.Value);
            
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            throw;
        }
    }

    public async Task<int> UpdateExpenseAsync(UpdateExpenseRequest request)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            
            using var command = new SqlCommand("dbo.sp_UpdateExpense", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@ExpenseId", request.ExpenseId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@AmountMinor", (int)(request.Amount * 100));
            command.Parameters.AddWithValue("@Currency", request.Currency);
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@ReceiptFile", (object?)request.ReceiptFile ?? DBNull.Value);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return reader.GetInt32(0);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expense");
            throw;
        }
        return 0;
    }

    public async Task<int> SubmitExpenseAsync(int expenseId)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            
            using var command = new SqlCommand("dbo.sp_SubmitExpense", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return reader.GetInt32(0);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting expense");
            throw;
        }
        return 0;
    }

    public async Task<int> ApproveExpenseAsync(int expenseId, int reviewerId)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            
            using var command = new SqlCommand("dbo.sp_ApproveExpense", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@ReviewerId", reviewerId);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return reader.GetInt32(0);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving expense");
            throw;
        }
        return 0;
    }

    public async Task<int> RejectExpenseAsync(int expenseId, int reviewerId)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            
            using var command = new SqlCommand("dbo.sp_RejectExpense", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@ReviewerId", reviewerId);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return reader.GetInt32(0);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting expense");
            throw;
        }
        return 0;
    }

    public async Task<int> DeleteExpenseAsync(int expenseId)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            
            using var command = new SqlCommand("dbo.sp_DeleteExpense", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return reader.GetInt32(0);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expense");
            throw;
        }
        return 0;
    }
}
