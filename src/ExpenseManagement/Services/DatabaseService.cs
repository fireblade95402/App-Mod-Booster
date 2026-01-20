using Microsoft.Data.SqlClient;
using Azure.Identity;
using ExpenseManagement.Models;
using System.Data;

namespace ExpenseManagement.Services;

public class DatabaseService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseService> _logger;

    public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private SqlConnection GetConnection()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        var managedIdentityClientId = _configuration["ManagedIdentityClientId"];
        
        var connection = new SqlConnection(connectionString);
        
        if (!string.IsNullOrEmpty(managedIdentityClientId))
        {
            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = managedIdentityClientId
            });
            var token = credential.GetToken(new Azure.Core.TokenRequestContext(
                new[] { "https://database.windows.net/.default" }));
            connection.AccessToken = token.Token;
        }
        
        return connection;
    }

    public async Task<List<Expense>> GetAllExpensesAsync()
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            using var command = new SqlCommand("GetAllExpenses", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            return await ReadExpensesAsync(command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all expenses");
            return GetDummyExpenses();
        }
    }

    public async Task<List<Expense>> GetExpensesByUserIdAsync(int userId)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            using var command = new SqlCommand("GetExpensesByUserId", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@UserId", userId);
            
            return await ReadExpensesAsync(command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expenses for user {UserId}", userId);
            return GetDummyExpenses();
        }
    }

    public async Task<List<Expense>> GetExpensesByStatusAsync(int statusId)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            using var command = new SqlCommand("GetExpensesByStatus", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@StatusId", statusId);
            
            return await ReadExpensesAsync(command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expenses by status {StatusId}", statusId);
            return GetDummyExpenses();
        }
    }

    public async Task<Expense?> GetExpenseByIdAsync(int expenseId)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            using var command = new SqlCommand("GetExpenseById", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            
            var expenses = await ReadExpensesAsync(command);
            return expenses.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expense {ExpenseId}", expenseId);
            return null;
        }
    }

    public async Task<List<Expense>> GetPendingExpensesAsync()
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            using var command = new SqlCommand("GetPendingExpenses", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            return await ReadExpensesAsync(command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending expenses");
            return GetDummyExpenses();
        }
    }

    public async Task<int> CreateExpenseAsync(CreateExpenseRequest request)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            using var command = new SqlCommand("CreateExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@UserId", request.UserId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@Amount", request.Amount);
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.AddWithValue("@Description", request.Description);
            command.Parameters.AddWithValue("@Receipt", (object?)request.Receipt ?? DBNull.Value);
            
            var newIdParam = new SqlParameter("@NewExpenseId", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(newIdParam);
            
            await command.ExecuteNonQueryAsync();
            return (int)newIdParam.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            throw new Exception($"Error creating expense: {ex.Message}", ex);
        }
    }

    public async Task<bool> SubmitExpenseAsync(int expenseId)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            using var command = new SqlCommand("SubmitExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            
            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting expense {ExpenseId}", expenseId);
            return false;
        }
    }

    public async Task<bool> ApproveExpenseAsync(int expenseId, int approvedBy, string? comments = null)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            using var command = new SqlCommand("ApproveExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@ApprovedBy", approvedBy);
            command.Parameters.AddWithValue("@Comments", (object?)comments ?? DBNull.Value);
            
            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving expense {ExpenseId}", expenseId);
            return false;
        }
    }

    public async Task<bool> RejectExpenseAsync(int expenseId, int rejectedBy, string? comments = null)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            using var command = new SqlCommand("RejectExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@RejectedBy", rejectedBy);
            command.Parameters.AddWithValue("@Comments", (object?)comments ?? DBNull.Value);
            
            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting expense {ExpenseId}", expenseId);
            return false;
        }
    }

    public async Task<bool> UpdateExpenseAsync(UpdateExpenseRequest request)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            using var command = new SqlCommand("UpdateExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@ExpenseId", request.ExpenseId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@Amount", request.Amount);
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.AddWithValue("@Description", request.Description);
            command.Parameters.AddWithValue("@Receipt", (object?)request.Receipt ?? DBNull.Value);
            
            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expense {ExpenseId}", request.ExpenseId);
            return false;
        }
    }

    public async Task<bool> DeleteExpenseAsync(int expenseId)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            using var command = new SqlCommand("DeleteExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            
            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expense {ExpenseId}", expenseId);
            return false;
        }
    }

    public async Task<List<ExpenseCategory>> GetAllCategoriesAsync()
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            using var command = new SqlCommand("GetAllCategories", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            var categories = new List<ExpenseCategory>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                categories.Add(new ExpenseCategory
                {
                    CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description"))
                });
            }
            return categories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories");
            return GetDummyCategories();
        }
    }

    public async Task<List<ExpenseStatus>> GetAllStatusesAsync()
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            using var command = new SqlCommand("GetAllStatuses", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            var statuses = new List<ExpenseStatus>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                statuses.Add(new ExpenseStatus
                {
                    StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
                    StatusName = reader.GetString(reader.GetOrdinal("StatusName"))
                });
            }
            return statuses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statuses");
            return GetDummyStatuses();
        }
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            using var command = new SqlCommand("GetAllUsers", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            var users = new List<User>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    Department = reader.IsDBNull(reader.GetOrdinal("Department")) ? null : reader.GetString(reader.GetOrdinal("Department")),
                    IsManager = reader.GetBoolean(reader.GetOrdinal("IsManager"))
                });
            }
            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return GetDummyUsers();
        }
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            using var command = new SqlCommand("GetUserById", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@UserId", userId);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new User
                {
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    Department = reader.IsDBNull(reader.GetOrdinal("Department")) ? null : reader.GetString(reader.GetOrdinal("Department")),
                    IsManager = reader.GetBoolean(reader.GetOrdinal("IsManager"))
                };
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", userId);
            return null;
        }
    }

    public async Task<Dictionary<string, object>> GetExpenseSummaryAsync()
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            using var command = new SqlCommand("GetExpenseSummary", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            var summary = new Dictionary<string, object>();
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    summary[reader.GetName(i)] = reader.GetValue(i);
                }
            }
            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expense summary");
            return new Dictionary<string, object>
            {
                { "TotalExpenses", 0 },
                { "TotalAmount", 0.0m },
                { "PendingCount", 0 },
                { "ApprovedCount", 0 }
            };
        }
    }

    private async Task<List<Expense>> ReadExpensesAsync(SqlCommand command)
    {
        var expenses = new List<Expense>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            expenses.Add(new Expense
            {
                ExpenseId = reader.GetInt32(reader.GetOrdinal("ExpenseId")),
                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                ExpenseDate = reader.GetDateTime(reader.GetOrdinal("ExpenseDate")),
                Description = reader.GetString(reader.GetOrdinal("Description")),
                Receipt = reader.IsDBNull(reader.GetOrdinal("Receipt")) ? null : reader.GetString(reader.GetOrdinal("Receipt")),
                StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                SubmittedDate = reader.IsDBNull(reader.GetOrdinal("SubmittedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("SubmittedDate")),
                ApprovedDate = reader.IsDBNull(reader.GetOrdinal("ApprovedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ApprovedDate")),
                ApprovedBy = reader.IsDBNull(reader.GetOrdinal("ApprovedBy")) ? null : reader.GetInt32(reader.GetOrdinal("ApprovedBy")),
                Comments = reader.IsDBNull(reader.GetOrdinal("Comments")) ? null : reader.GetString(reader.GetOrdinal("Comments")),
                CategoryName = TryGetString(reader, "CategoryName"),
                StatusName = TryGetString(reader, "StatusName"),
                UserName = TryGetString(reader, "UserName"),
                ApproverName = TryGetString(reader, "ApproverName")
            });
        }
        return expenses;
    }

    private string? TryGetString(SqlDataReader reader, string columnName)
    {
        try
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }
        catch
        {
            return null;
        }
    }

    private List<Expense> GetDummyExpenses()
    {
        return new List<Expense>
        {
            new Expense
            {
                ExpenseId = 1,
                UserId = 1,
                CategoryId = 1,
                Amount = 250.00m,
                ExpenseDate = DateTime.Now.AddDays(-5),
                Description = "Client lunch meeting",
                StatusId = 2,
                CreatedDate = DateTime.Now.AddDays(-5),
                CategoryName = "Meals",
                StatusName = "Pending",
                UserName = "John Doe"
            },
            new Expense
            {
                ExpenseId = 2,
                UserId = 1,
                CategoryId = 2,
                Amount = 125.50m,
                ExpenseDate = DateTime.Now.AddDays(-3),
                Description = "Taxi to airport",
                StatusId = 3,
                CreatedDate = DateTime.Now.AddDays(-3),
                ApprovedDate = DateTime.Now.AddDays(-1),
                CategoryName = "Travel",
                StatusName = "Approved",
                UserName = "John Doe"
            }
        };
    }

    private List<ExpenseCategory> GetDummyCategories()
    {
        return new List<ExpenseCategory>
        {
            new ExpenseCategory { CategoryId = 1, CategoryName = "Meals", Description = "Business meals and entertainment" },
            new ExpenseCategory { CategoryId = 2, CategoryName = "Travel", Description = "Transportation and lodging" },
            new ExpenseCategory { CategoryId = 3, CategoryName = "Supplies", Description = "Office supplies" },
            new ExpenseCategory { CategoryId = 4, CategoryName = "Other", Description = "Miscellaneous expenses" }
        };
    }

    private List<ExpenseStatus> GetDummyStatuses()
    {
        return new List<ExpenseStatus>
        {
            new ExpenseStatus { StatusId = 1, StatusName = "Draft" },
            new ExpenseStatus { StatusId = 2, StatusName = "Pending" },
            new ExpenseStatus { StatusId = 3, StatusName = "Approved" },
            new ExpenseStatus { StatusId = 4, StatusName = "Rejected" }
        };
    }

    private List<User> GetDummyUsers()
    {
        return new List<User>
        {
            new User { UserId = 1, FirstName = "John", LastName = "Doe", Email = "john.doe@company.com", Department = "Sales", IsManager = false },
            new User { UserId = 2, FirstName = "Jane", LastName = "Smith", Email = "jane.smith@company.com", Department = "Sales", IsManager = true }
        };
    }
}
