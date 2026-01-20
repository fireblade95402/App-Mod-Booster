using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly DatabaseService _databaseService;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(DatabaseService databaseService, ILogger<CategoriesController> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<ExpenseCategory>>> GetAllCategories()
    {
        try
        {
            var categories = await _databaseService.GetAllCategoriesAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("statuses")]
    public async Task<ActionResult<List<ExpenseStatus>>> GetAllStatuses()
    {
        try
        {
            var statuses = await _databaseService.GetAllStatusesAsync();
            return Ok(statuses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statuses");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
