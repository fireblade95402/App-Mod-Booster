using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly DatabaseService _databaseService;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(DatabaseService databaseService, ILogger<ExpensesController> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<Expense>>> GetAllExpenses()
    {
        try
        {
            var expenses = await _databaseService.GetAllExpensesAsync();
            return Ok(expenses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all expenses");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<List<Expense>>> GetExpensesByUserId(int userId)
    {
        try
        {
            var expenses = await _databaseService.GetExpensesByUserIdAsync(userId);
            return Ok(expenses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expenses for user {UserId}", userId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("status/{statusId}")]
    public async Task<ActionResult<List<Expense>>> GetExpensesByStatus(int statusId)
    {
        try
        {
            var expenses = await _databaseService.GetExpensesByStatusAsync(statusId);
            return Ok(expenses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expenses by status {StatusId}", statusId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Expense>> GetExpenseById(int id)
    {
        try
        {
            var expense = await _databaseService.GetExpenseByIdAsync(id);
            if (expense == null)
                return NotFound();
            return Ok(expense);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expense {ExpenseId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("pending")]
    public async Task<ActionResult<List<Expense>>> GetPendingExpenses()
    {
        try
        {
            var expenses = await _databaseService.GetPendingExpensesAsync();
            return Ok(expenses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending expenses");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<int>> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        try
        {
            var expenseId = await _databaseService.CreateExpenseAsync(request);
            return CreatedAtAction(nameof(GetExpenseById), new { id = expenseId }, expenseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("{id}/submit")]
    public async Task<ActionResult> SubmitExpense(int id)
    {
        try
        {
            var result = await _databaseService.SubmitExpenseAsync(id);
            if (!result)
                return BadRequest(new { error = "Failed to submit expense" });
            return Ok(new { message = "Expense submitted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting expense {ExpenseId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("{id}/approve")]
    public async Task<ActionResult> ApproveExpense(int id, [FromBody] ApprovalRequest request)
    {
        try
        {
            var result = await _databaseService.ApproveExpenseAsync(id, request.ApprovedBy, request.Comments);
            if (!result)
                return BadRequest(new { error = "Failed to approve expense" });
            return Ok(new { message = "Expense approved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving expense {ExpenseId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("{id}/reject")]
    public async Task<ActionResult> RejectExpense(int id, [FromBody] ApprovalRequest request)
    {
        try
        {
            var result = await _databaseService.RejectExpenseAsync(id, request.ApprovedBy, request.Comments);
            if (!result)
                return BadRequest(new { error = "Failed to reject expense" });
            return Ok(new { message = "Expense rejected successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting expense {ExpenseId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateExpense(int id, [FromBody] UpdateExpenseRequest request)
    {
        try
        {
            if (id != request.ExpenseId)
                return BadRequest(new { error = "ID mismatch" });

            var result = await _databaseService.UpdateExpenseAsync(request);
            if (!result)
                return BadRequest(new { error = "Failed to update expense" });
            return Ok(new { message = "Expense updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expense {ExpenseId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteExpense(int id)
    {
        try
        {
            var result = await _databaseService.DeleteExpenseAsync(id);
            if (!result)
                return BadRequest(new { error = "Failed to delete expense" });
            return Ok(new { message = "Expense deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expense {ExpenseId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("summary")]
    public async Task<ActionResult<Dictionary<string, object>>> GetExpenseSummary()
    {
        try
        {
            var summary = await _databaseService.GetExpenseSummaryAsync();
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expense summary");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class ApprovalRequest
{
    public int ApprovedBy { get; set; }
    public string? Comments { get; set; }
}
