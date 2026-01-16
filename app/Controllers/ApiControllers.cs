using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExpensesController : ControllerBase
{
    private readonly ExpenseService _expenseService;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(ExpenseService expenseService, ILogger<ExpensesController> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    /// <summary>
    /// Get all expenses with optional filters
    /// </summary>
    /// <param name="userId">Filter by user ID (optional)</param>
    /// <param name="statusId">Filter by status ID (optional)</param>
    /// <param name="categoryId">Filter by category ID (optional)</param>
    [HttpGet]
    public async Task<ActionResult<List<Expense>>> GetExpenses(
        [FromQuery] int? userId = null, 
        [FromQuery] int? statusId = null, 
        [FromQuery] int? categoryId = null)
    {
        try
        {
            var expenses = await _expenseService.GetExpensesAsync(userId, statusId, categoryId);
            return Ok(expenses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expenses");
            return StatusCode(500, new { error = "Failed to retrieve expenses", details = ex.Message });
        }
    }

    /// <summary>
    /// Get expense by ID
    /// </summary>
    /// <param name="id">Expense ID</param>
    [HttpGet("{id}")]
    public async Task<ActionResult<Expense>> GetExpenseById(int id)
    {
        try
        {
            var expense = await _expenseService.GetExpenseByIdAsync(id);
            if (expense == null)
            {
                return NotFound(new { error = $"Expense with ID {id} not found" });
            }
            return Ok(expense);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expense {ExpenseId}", id);
            return StatusCode(500, new { error = "Failed to retrieve expense", details = ex.Message });
        }
    }

    /// <summary>
    /// Get pending expenses for approval
    /// </summary>
    /// <param name="managerId">Filter by manager ID (optional)</param>
    [HttpGet("pending")]
    public async Task<ActionResult<List<Expense>>> GetPendingExpenses([FromQuery] int? managerId = null)
    {
        try
        {
            var expenses = await _expenseService.GetPendingExpensesAsync(managerId);
            return Ok(expenses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending expenses");
            return StatusCode(500, new { error = "Failed to retrieve pending expenses", details = ex.Message });
        }
    }

    /// <summary>
    /// Get expense summary statistics
    /// </summary>
    /// <param name="userId">Filter by user ID (optional)</param>
    [HttpGet("summary")]
    public async Task<ActionResult<ExpenseSummary>> GetExpenseSummary([FromQuery] int? userId = null)
    {
        try
        {
            var summary = await _expenseService.GetExpenseSummaryAsync(userId);
            if (summary == null)
            {
                return NotFound(new { error = "Summary not found" });
            }
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expense summary");
            return StatusCode(500, new { error = "Failed to retrieve expense summary", details = ex.Message });
        }
    }

    /// <summary>
    /// Create a new expense
    /// </summary>
    /// <param name="request">Expense creation request</param>
    [HttpPost]
    public async Task<ActionResult<Expense>> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        try
        {
            var expense = await _expenseService.CreateExpenseAsync(request);
            if (expense == null)
            {
                return BadRequest(new { error = "Failed to create expense" });
            }
            return CreatedAtAction(nameof(GetExpenseById), new { id = expense.ExpenseId }, expense);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            return StatusCode(500, new { error = "Failed to create expense", details = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing expense
    /// </summary>
    /// <param name="id">Expense ID</param>
    /// <param name="request">Expense update request</param>
    [HttpPut("{id}")]
    public async Task<ActionResult<Expense>> UpdateExpense(int id, [FromBody] UpdateExpenseRequest request)
    {
        try
        {
            request.ExpenseId = id;
            var expense = await _expenseService.UpdateExpenseAsync(request);
            if (expense == null)
            {
                return NotFound(new { error = $"Expense with ID {id} not found" });
            }
            return Ok(expense);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expense {ExpenseId}", id);
            return StatusCode(500, new { error = "Failed to update expense", details = ex.Message });
        }
    }

    /// <summary>
    /// Submit an expense for approval
    /// </summary>
    /// <param name="id">Expense ID</param>
    [HttpPost("{id}/submit")]
    public async Task<ActionResult<Expense>> SubmitExpense(int id)
    {
        try
        {
            var expense = await _expenseService.SubmitExpenseAsync(id);
            if (expense == null)
            {
                return NotFound(new { error = $"Expense with ID {id} not found" });
            }
            return Ok(expense);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting expense {ExpenseId}", id);
            return StatusCode(500, new { error = "Failed to submit expense", details = ex.Message });
        }
    }

    /// <summary>
    /// Approve an expense (Manager action)
    /// </summary>
    /// <param name="id">Expense ID</param>
    /// <param name="request">Approval request with reviewer ID</param>
    [HttpPost("{id}/approve")]
    public async Task<ActionResult<Expense>> ApproveExpense(int id, [FromBody] ApproveRejectExpenseRequest request)
    {
        try
        {
            request.ExpenseId = id;
            var expense = await _expenseService.ApproveExpenseAsync(request);
            if (expense == null)
            {
                return NotFound(new { error = $"Expense with ID {id} not found" });
            }
            return Ok(expense);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving expense {ExpenseId}", id);
            return StatusCode(500, new { error = "Failed to approve expense", details = ex.Message });
        }
    }

    /// <summary>
    /// Reject an expense (Manager action)
    /// </summary>
    /// <param name="id">Expense ID</param>
    /// <param name="request">Rejection request with reviewer ID</param>
    [HttpPost("{id}/reject")]
    public async Task<ActionResult<Expense>> RejectExpense(int id, [FromBody] ApproveRejectExpenseRequest request)
    {
        try
        {
            request.ExpenseId = id;
            var expense = await _expenseService.RejectExpenseAsync(request);
            if (expense == null)
            {
                return NotFound(new { error = $"Expense with ID {id} not found" });
            }
            return Ok(expense);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting expense {ExpenseId}", id);
            return StatusCode(500, new { error = "Failed to reject expense", details = ex.Message });
        }
    }

    /// <summary>
    /// Delete an expense
    /// </summary>
    /// <param name="id">Expense ID</param>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteExpense(int id)
    {
        try
        {
            var result = await _expenseService.DeleteExpenseAsync(id);
            if (result)
            {
                return Ok(new { message = $"Expense {id} deleted successfully" });
            }
            return NotFound(new { error = $"Expense with ID {id} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expense {ExpenseId}", id);
            return StatusCode(500, new { error = "Failed to delete expense", details = ex.Message });
        }
    }
}

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly ExpenseService _expenseService;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(ExpenseService expenseService, ILogger<CategoriesController> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    /// <summary>
    /// Get all expense categories
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ExpenseCategory>>> GetCategories()
    {
        try
        {
            var categories = await _expenseService.GetExpenseCategoriesAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories");
            return StatusCode(500, new { error = "Failed to retrieve categories", details = ex.Message });
        }
    }
}

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class StatusesController : ControllerBase
{
    private readonly ExpenseService _expenseService;
    private readonly ILogger<StatusesController> _logger;

    public StatusesController(ExpenseService expenseService, ILogger<StatusesController> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    /// <summary>
    /// Get all expense statuses
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ExpenseStatus>>> GetStatuses()
    {
        try
        {
            var statuses = await _expenseService.GetExpenseStatusesAsync();
            return Ok(statuses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statuses");
            return StatusCode(500, new { error = "Failed to retrieve statuses", details = ex.Message });
        }
    }
}

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly ExpenseService _expenseService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(ExpenseService expenseService, ILogger<UsersController> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<User>>> GetUsers()
    {
        try
        {
            var users = await _expenseService.GetUsersAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return StatusCode(500, new { error = "Failed to retrieve users", details = ex.Message });
        }
    }
}
