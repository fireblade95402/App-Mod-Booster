using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Services;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(ChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint to verify if GenAI services are available
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        if (_chatService.IsAvailable)
        {
            return Ok(new { status = "available", message = "Chat service is ready" });
        }
        else
        {
            return StatusCode(503, new { status = "unavailable", message = "GenAI services are not deployed" });
        }
    }

    /// <summary>
    /// Send a message to the AI assistant
    /// </summary>
    [HttpPost("message")]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { error = "Message cannot be empty" });
        }

        if (!_chatService.IsAvailable)
        {
            return StatusCode(503, new
            {
                error = "GenAI services not available",
                message = "Azure OpenAI resources are not deployed. Use deploy-with-chat.sh to deploy them."
            });
        }

        try
        {
            var response = await _chatService.ProcessMessageAsync(request.Message);
            return Ok(new { response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return StatusCode(500, new { error = "Error processing message", details = ex.Message });
        }
    }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
}
