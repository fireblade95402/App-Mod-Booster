using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages;

public class ChatModel : PageModel
{
    private readonly IConfiguration _configuration;

    public string ChatStatus { get; set; } = string.Empty;

    public ChatModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void OnGet()
    {
        var openAIEndpoint = _configuration["OpenAI:Endpoint"];
        
        if (string.IsNullOrEmpty(openAIEndpoint))
        {
            ChatStatus = "GenAI services are not deployed. Run 'deploy-with-chat.sh' to enable full AI chat functionality.";
        }
        else
        {
            ChatStatus = "Chat is ready! GenAI services are configured and available.";
        }
    }
}
