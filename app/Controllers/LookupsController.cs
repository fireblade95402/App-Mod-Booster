using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly DatabaseService _dbService;
    private readonly ErrorHandlingService _errorService;

    public CategoriesController(DatabaseService dbService, ErrorHandlingService errorService)
    {
        _dbService = dbService;
        _errorService = errorService;
    }

    /// <summary>
    /// Get all expense categories
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ExpenseCategory>>> GetCategories()
    {
        try
        {
            var categories = await _dbService.GetExpenseCategoriesAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.FormatDatabaseError(ex, "CategoriesController", "GetCategories");
            return StatusCode(503, new { error = errorMessage, dummyData = _errorService.GetDummyCategories() });
        }
    }
}

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly DatabaseService _dbService;
    private readonly ErrorHandlingService _errorService;

    public UsersController(DatabaseService dbService, ErrorHandlingService errorService)
    {
        _dbService = dbService;
        _errorService = errorService;
    }

    /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<User>>> GetUsers()
    {
        try
        {
            var users = await _dbService.GetUsersAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.FormatDatabaseError(ex, "UsersController", "GetUsers");
            return StatusCode(503, new { error = errorMessage, dummyData = _errorService.GetDummyUsers() });
        }
    }
}

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class StatusesController : ControllerBase
{
    private readonly DatabaseService _dbService;
    private readonly ErrorHandlingService _errorService;

    public StatusesController(DatabaseService dbService, ErrorHandlingService errorService)
    {
        _dbService = dbService;
        _errorService = errorService;
    }

    /// <summary>
    /// Get all expense statuses
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ExpenseStatus>>> GetStatuses()
    {
        try
        {
            var statuses = await _dbService.GetExpenseStatusesAsync();
            return Ok(statuses);
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.FormatDatabaseError(ex, "StatusesController", "GetStatuses");
            return StatusCode(503, new { error = errorMessage });
        }
    }
}
