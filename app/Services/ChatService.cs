using Azure.AI.OpenAI;
using Azure;
using Azure.Identity;
using OpenAI.Chat;
using ExpenseManagement.Models;
using System.Text.Json;
using System.ClientModel;

namespace ExpenseManagement.Services;

public class ChatService
{
    private readonly IConfiguration _configuration;
    private readonly DatabaseService _dbService;
    private readonly ILogger<ChatService> _logger;
    private readonly AzureOpenAIClient? _openAIClient;
    private readonly string? _deploymentName;

    public ChatService(IConfiguration configuration, DatabaseService dbService, ILogger<ChatService> logger)
    {
        _configuration = configuration;
        _dbService = dbService;
        _logger = logger;

        var endpoint = _configuration["OpenAI:Endpoint"];
        _deploymentName = _configuration["OpenAI:DeploymentName"];
        var managedIdentityClientId = _configuration["ManagedIdentityClientId"];

        if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(_deploymentName))
        {
            try
            {
                Azure.Core.TokenCredential credential;
                
                if (!string.IsNullOrEmpty(managedIdentityClientId))
                {
                    _logger.LogInformation("Using ManagedIdentityCredential with client ID: {ClientId}", managedIdentityClientId);
                    credential = new ManagedIdentityCredential(managedIdentityClientId);
                }
                else
                {
                    _logger.LogInformation("Using DefaultAzureCredential");
                    credential = new DefaultAzureCredential();
                }

                _openAIClient = new AzureOpenAIClient(new Uri(endpoint), credential);
                _logger.LogInformation("Azure OpenAI client initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Azure OpenAI client");
            }
        }
        else
        {
            _logger.LogWarning("OpenAI configuration not found. Chat will return dummy responses.");
        }
    }

    public async Task<string> GetChatResponseAsync(string userMessage)
    {
        if (_openAIClient == null || string.IsNullOrEmpty(_deploymentName))
        {
            return "GenAI services not deployed. Please run 'deploy-with-chat.sh' to enable AI chat functionality. " +
                   "For now, you can use the API endpoints directly at /swagger to interact with expenses.";
        }

        try
        {
            var chatClient = _openAIClient.GetChatClient(_deploymentName);
            
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(@"You are a helpful assistant for an Expense Management System. 
You can help users with:
- Viewing expenses (all, by status, by user)
- Getting expense details
- Creating new expenses
- Submitting expenses for approval
- Approving or rejecting expenses
- Getting expense summaries

Use the available functions to interact with the database. Always provide clear, helpful responses.
When listing expenses, format them nicely with bullet points or numbered lists.
Always include the expense ID, user, category, amount, date, and status in your responses."),
                new UserChatMessage(userMessage)
            };

            var tools = new List<ChatTool>
            {
                ChatTool.CreateFunctionTool(
                    functionName: "get_expenses",
                    functionDescription: "Get all expenses, optionally filtered by status (Draft, Submitted, Approved, Rejected) or user ID",
                    functionParameters: BinaryData.FromString("""
                    {
                        "type": "object",
                        "properties": {
                            "status": {
                                "type": "string",
                                "description": "Filter by status: Draft, Submitted, Approved, or Rejected",
                                "enum": ["Draft", "Submitted", "Approved", "Rejected"]
                            },
                            "userId": {
                                "type": "integer",
                                "description": "Filter by user ID"
                            }
                        }
                    }
                    """)
                ),
                ChatTool.CreateFunctionTool(
                    functionName: "get_expense_by_id",
                    functionDescription: "Get details of a specific expense by its ID",
                    functionParameters: BinaryData.FromString("""
                    {
                        "type": "object",
                        "properties": {
                            "expenseId": {
                                "type": "integer",
                                "description": "The ID of the expense"
                            }
                        },
                        "required": ["expenseId"]
                    }
                    """)
                ),
                ChatTool.CreateFunctionTool(
                    functionName: "get_categories",
                    functionDescription: "Get all expense categories",
                    functionParameters: BinaryData.FromString("""
                    {
                        "type": "object",
                        "properties": {}
                    }
                    """)
                ),
                ChatTool.CreateFunctionTool(
                    functionName: "get_users",
                    functionDescription: "Get all users in the system",
                    functionParameters: BinaryData.FromString("""
                    {
                        "type": "object",
                        "properties": {}
                    }
                    """)
                )
            };

            var options = new ChatCompletionOptions();
            foreach (var tool in tools)
            {
                options.Tools.Add(tool);
            }

            // Keep calling until we get a final response (no more tool calls)
            int maxIterations = 5;
            int iteration = 0;

            while (iteration < maxIterations)
            {
                iteration++;
                
                var completion = await chatClient.CompleteChatAsync(messages, options);
                var responseMessage = completion.Value.Content[0];
                
                // Check if there are tool calls
                if (completion.Value.FinishReason == ChatFinishReason.ToolCalls)
                {
                    // Add the assistant's message with tool calls
                    messages.Add(new AssistantChatMessage(completion.Value));

                    // Execute each tool call
                    foreach (var toolCall in completion.Value.ToolCalls)
                    {
                        var functionName = toolCall.FunctionName;
                        var functionArgs = toolCall.FunctionArguments;

                        _logger.LogInformation("Executing function: {FunctionName} with args: {Args}", functionName, functionArgs);

                        string functionResult = await ExecuteFunctionAsync(functionName, functionArgs);
                        
                        messages.Add(new ToolChatMessage(toolCall.Id, functionResult));
                    }
                }
                else
                {
                    // We have a final response
                    return responseMessage.Text;
                }
            }

            return "I apologize, but I couldn't complete your request. Please try rephrasing your question.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat response");
            return $"Error: {ex.Message}. Please try again or use the API endpoints at /swagger.";
        }
    }

    private async Task<string> ExecuteFunctionAsync(string functionName, BinaryData functionArgs)
    {
        try
        {
            var argsJson = functionArgs.ToString();
            
            switch (functionName)
            {
                case "get_expenses":
                    var getExpensesArgs = JsonSerializer.Deserialize<GetExpensesArgs>(argsJson);
                    var expenses = await _dbService.GetExpensesAsync(getExpensesArgs?.status, getExpensesArgs?.userId);
                    return JsonSerializer.Serialize(expenses);

                case "get_expense_by_id":
                    var getExpenseArgs = JsonSerializer.Deserialize<GetExpenseByIdArgs>(argsJson);
                    if (getExpenseArgs?.expenseId == null)
                        return JsonSerializer.Serialize(new { error = "Expense ID is required" });
                    
                    var expense = await _dbService.GetExpenseByIdAsync(getExpenseArgs.expenseId);
                    return JsonSerializer.Serialize(expense);

                case "get_categories":
                    var categories = await _dbService.GetExpenseCategoriesAsync();
                    return JsonSerializer.Serialize(categories);

                case "get_users":
                    var users = await _dbService.GetUsersAsync();
                    return JsonSerializer.Serialize(users);

                default:
                    return JsonSerializer.Serialize(new { error = $"Unknown function: {functionName}" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function {FunctionName}", functionName);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private class GetExpensesArgs
    {
        public string? status { get; set; }
        public int? userId { get; set; }
    }

    private class GetExpenseByIdArgs
    {
        public int expenseId { get; set; }
    }
}
