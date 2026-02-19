using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExpensesController : ControllerBase
{
    private readonly DatabaseService _dbService;
    private readonly ErrorHandlingService _errorService;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(DatabaseService dbService, ErrorHandlingService errorService, ILogger<ExpensesController> logger)
    {
        _dbService = dbService;
        _errorService = errorService;
        _logger = logger;
    }

    /// <summary>
    /// Get all expenses, optionally filtered by status and/or user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Expense>>> GetExpenses([FromQuery] string? status = null, [FromQuery] int? userId = null)
    {
        try
        {
            var expenses = await _dbService.GetExpensesAsync(status, userId);
            return Ok(expenses);
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.FormatDatabaseError(ex, "ExpensesController", "GetExpenses");
            return StatusCode(503, new { error = errorMessage, dummyData = _errorService.GetDummyExpenses() });
        }
    }

    /// <summary>
    /// Get expense by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Expense>> GetExpense(int id)
    {
        try
        {
            var expense = await _dbService.GetExpenseByIdAsync(id);
            if (expense == null)
                return NotFound();
            return Ok(expense);
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.FormatDatabaseError(ex, "ExpensesController", "GetExpense");
            return StatusCode(503, new { error = errorMessage });
        }
    }

    /// <summary>
    /// Create a new expense
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<int>> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        try
        {
            var expenseId = await _dbService.CreateExpenseAsync(request);
            return CreatedAtAction(nameof(GetExpense), new { id = expenseId }, new { expenseId });
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.FormatDatabaseError(ex, "ExpensesController", "CreateExpense");
            return StatusCode(503, new { error = errorMessage });
        }
    }

    /// <summary>
    /// Update an existing expense
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateExpense(int id, [FromBody] UpdateExpenseRequest request)
    {
        try
        {
            request.ExpenseId = id;
            var rowsAffected = await _dbService.UpdateExpenseAsync(request);
            if (rowsAffected == 0)
                return NotFound();
            return NoContent();
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.FormatDatabaseError(ex, "ExpensesController", "UpdateExpense");
            return StatusCode(503, new { error = errorMessage });
        }
    }

    /// <summary>
    /// Submit an expense for approval
    /// </summary>
    [HttpPost("{id}/submit")]
    public async Task<ActionResult> SubmitExpense(int id)
    {
        try
        {
            var rowsAffected = await _dbService.SubmitExpenseAsync(id);
            if (rowsAffected == 0)
                return NotFound();
            return NoContent();
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.FormatDatabaseError(ex, "ExpensesController", "SubmitExpense");
            return StatusCode(503, new { error = errorMessage });
        }
    }

    /// <summary>
    /// Approve an expense
    /// </summary>
    [HttpPost("{id}/approve")]
    public async Task<ActionResult> ApproveExpense(int id, [FromQuery] int reviewerId)
    {
        try
        {
            var rowsAffected = await _dbService.ApproveExpenseAsync(id, reviewerId);
            if (rowsAffected == 0)
                return NotFound();
            return NoContent();
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.FormatDatabaseError(ex, "ExpensesController", "ApproveExpense");
            return StatusCode(503, new { error = errorMessage });
        }
    }

    /// <summary>
    /// Reject an expense
    /// </summary>
    [HttpPost("{id}/reject")]
    public async Task<ActionResult> RejectExpense(int id, [FromQuery] int reviewerId)
    {
        try
        {
            var rowsAffected = await _dbService.RejectExpenseAsync(id, reviewerId);
            if (rowsAffected == 0)
                return NotFound();
            return NoContent();
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.FormatDatabaseError(ex, "ExpensesController", "RejectExpense");
            return StatusCode(503, new { error = errorMessage });
        }
    }

    /// <summary>
    /// Delete a draft expense
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteExpense(int id)
    {
        try
        {
            var rowsAffected = await _dbService.DeleteExpenseAsync(id);
            if (rowsAffected == 0)
                return NotFound();
            return NoContent();
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.FormatDatabaseError(ex, "ExpensesController", "DeleteExpense");
            return StatusCode(503, new { error = errorMessage });
        }
    }
}
