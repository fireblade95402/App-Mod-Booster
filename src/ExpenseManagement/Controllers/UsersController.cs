using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly DatabaseService _databaseService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(DatabaseService databaseService, ILogger<UsersController> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<User>>> GetAllUsers()
    {
        try
        {
            var users = await _databaseService.GetAllUsersAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUserById(int id)
    {
        try
        {
            var user = await _databaseService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
