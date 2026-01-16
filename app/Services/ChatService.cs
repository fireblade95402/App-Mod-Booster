using Azure.AI.OpenAI;
using Azure;
using Azure.Identity;
using ExpenseManagement.Models;
using System.Text.Json;
using OpenAI.Chat;
using System.ClientModel;

namespace ExpenseManagement.Services;

public class ChatService
{
    private readonly ExpenseService _expenseService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatService> _logger;
    private readonly ChatClient? _chatClient;
    private readonly bool _isAvailable;

    public bool IsAvailable => _isAvailable;

    public ChatService(ExpenseService expenseService, IConfiguration configuration, ILogger<ChatService> logger)
    {
        _expenseService = expenseService;
        _configuration = configuration;
        _logger = logger;

        try
        {
            var endpoint = _configuration["OpenAI__Endpoint"];
            var deploymentName = _configuration["OpenAI__DeploymentName"];
            var managedIdentityClientId = _configuration["ManagedIdentityClientId"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(deploymentName))
            {
                _logger.LogWarning("OpenAI configuration not found. Chat functionality will be disabled.");
                _isAvailable = false;
                return;
            }

            // Use ManagedIdentityCredential with explicit client ID
            ApiKeyCredential credential;
            
            if (!string.IsNullOrEmpty(managedIdentityClientId))
            {
                _logger.LogInformation("Using ManagedIdentityCredential with client ID: {ClientId}", managedIdentityClientId);
                var tokenCredential = new ManagedIdentityCredential(managedIdentityClientId);
                var token = tokenCredential.GetToken(new Azure.Core.TokenRequestContext(new[] { "https://cognitiveservices.azure.com/.default" }), default);
                credential = new ApiKeyCredential(token.Token);
            }
            else
            {
                _logger.LogInformation("Using DefaultAzureCredential");
                var tokenCredential = new DefaultAzureCredential();
                var token = tokenCredential.GetToken(new Azure.Core.TokenRequestContext(new[] { "https://cognitiveservices.azure.com/.default" }), default);
                credential = new ApiKeyCredential(token.Token);
            }

            var azureClient = new AzureOpenAIClient(new Uri(endpoint), credential);
            _chatClient = azureClient.GetChatClient(deploymentName);
            _isAvailable = true;

            _logger.LogInformation("Chat service initialized successfully with endpoint: {Endpoint}", endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize chat service");
            _isAvailable = false;
        }
    }

    public async Task<string> ProcessMessageAsync(string userMessage)
    {
        if (!_isAvailable || _chatClient == null)
        {
            return "Chat service is not available. Please ensure Azure OpenAI resources are deployed.";
        }

        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(
                    "You are an AI assistant for an Expense Management System. " +
                    "You can help users view expenses, create expenses, approve expenses, and manage expense data. " +
                    "When users ask to list or view data, format your responses in a clear, readable format using numbered or bulleted lists. " +
                    "You have access to the following functions to interact with the database:\n" +
                    "- get_expenses: Get all expenses with optional filters (userId, statusId, categoryId)\n" +
                    "- get_pending_expenses: Get pending expenses awaiting approval\n" +
                    "- get_expense_categories: Get all expense categories\n" +
                    "- get_expense_statuses: Get all expense statuses\n" +
                    "- get_users: Get all users\n" +
                    "- create_expense: Create a new expense\n" +
                    "- approve_expense: Approve an expense (requires expenseId and reviewedBy)\n" +
                    "- reject_expense: Reject an expense (requires expenseId and reviewedBy)\n" +
                    "Always be helpful and provide clear, formatted responses."
                ),
                new UserChatMessage(userMessage)
            };

            // Define available functions
            var tools = new List<ChatTool>
            {
                ChatTool.CreateFunctionTool(
                    "get_expenses",
                    "Retrieves all expenses from the database with optional filters",
                    BinaryData.FromString("""
                    {
                        "type": "object",
                        "properties": {
                            "userId": {"type": "integer", "description": "Filter by user ID"},
                            "statusId": {"type": "integer", "description": "Filter by status ID"},
                            "categoryId": {"type": "integer", "description": "Filter by category ID"}
                        }
                    }
                    """)
                ),
                ChatTool.CreateFunctionTool(
                    "get_pending_expenses",
                    "Retrieves all pending expenses awaiting manager approval"
                ),
                ChatTool.CreateFunctionTool(
                    "get_expense_categories",
                    "Retrieves all expense categories"
                ),
                ChatTool.CreateFunctionTool(
                    "get_expense_statuses",
                    "Retrieves all expense statuses"
                ),
                ChatTool.CreateFunctionTool(
                    "get_users",
                    "Retrieves all users in the system"
                ),
                ChatTool.CreateFunctionTool(
                    "create_expense",
                    "Creates a new expense",
                    BinaryData.FromString("""
                    {
                        "type": "object",
                        "required": ["userId", "categoryId", "amountGBP", "expenseDate"],
                        "properties": {
                            "userId": {"type": "integer", "description": "User ID who is creating the expense"},
                            "categoryId": {"type": "integer", "description": "Category ID for the expense"},
                            "amountGBP": {"type": "number", "description": "Amount in GBP"},
                            "expenseDate": {"type": "string", "format": "date", "description": "Expense date (YYYY-MM-DD)"},
                            "description": {"type": "string", "description": "Expense description"},
                            "statusName": {"type": "string", "enum": ["Draft", "Submitted"], "description": "Initial status (default: Submitted)"}
                        }
                    }
                    """)
                )
            };

            var options = new ChatCompletionOptions();
            foreach (var tool in tools)
            {
                options.Tools.Add(tool);
            }

            // Get initial response
            var response = await _chatClient.CompleteChatAsync(messages, options);
            var completion = response.Value;

            // Handle function calling
            while (completion.FinishReason == ChatFinishReason.ToolCalls)
            {
                // Add assistant's response to messages
                messages.Add(new AssistantChatMessage(completion));

                // Process each tool call
                foreach (var toolCall in completion.ToolCalls)
                {
                    var functionName = toolCall.FunctionName;
                    var functionArgs = toolCall.FunctionArguments.ToString();
                    
                    _logger.LogInformation("Function call: {FunctionName} with args: {Args}", functionName, functionArgs);

                    // Execute the function
                    var functionResult = await ExecuteFunctionAsync(functionName, functionArgs);
                    
                    // Add function result to messages
                    messages.Add(new ToolChatMessage(toolCall.Id, functionResult));
                }

                // Get next response
                response = await _chatClient.CompleteChatAsync(messages, options);
                completion = response.Value;
            }

            return completion.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return $"Error: {ex.Message}";
        }
    }

    private async Task<string> ExecuteFunctionAsync(string functionName, string argumentsJson)
    {
        try
        {
            switch (functionName)
            {
                case "get_expenses":
                    var expensesArgs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson);
                    var userId = expensesArgs?.ContainsKey("userId") == true ? (int?)expensesArgs["userId"].GetInt32() : null;
                    var statusId = expensesArgs?.ContainsKey("statusId") == true ? (int?)expensesArgs["statusId"].GetInt32() : null;
                    var categoryId = expensesArgs?.ContainsKey("categoryId") == true ? (int?)expensesArgs["categoryId"].GetInt32() : null;
                    var expenses = await _expenseService.GetExpensesAsync(userId, statusId, categoryId);
                    return JsonSerializer.Serialize(expenses);

                case "get_pending_expenses":
                    var pendingExpenses = await _expenseService.GetPendingExpensesAsync();
                    return JsonSerializer.Serialize(pendingExpenses);

                case "get_expense_categories":
                    var categories = await _expenseService.GetExpenseCategoriesAsync();
                    return JsonSerializer.Serialize(categories);

                case "get_expense_statuses":
                    var statuses = await _expenseService.GetExpenseStatusesAsync();
                    return JsonSerializer.Serialize(statuses);

                case "get_users":
                    var users = await _expenseService.GetUsersAsync();
                    return JsonSerializer.Serialize(users);

                case "create_expense":
                    var createArgs = JsonSerializer.Deserialize<CreateExpenseRequest>(argumentsJson);
                    if (createArgs != null)
                    {
                        var createdExpense = await _expenseService.CreateExpenseAsync(createArgs);
                        return JsonSerializer.Serialize(createdExpense);
                    }
                    return JsonSerializer.Serialize(new { error = "Invalid arguments" });

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
}
