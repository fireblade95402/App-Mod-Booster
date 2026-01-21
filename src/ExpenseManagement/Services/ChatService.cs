using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using ExpenseManagement.Models;
using System.Text.Json;

namespace ExpenseManagement.Services;

public class ChatService
{
    private readonly IConfiguration _configuration;
    private readonly DatabaseService _databaseService;
    private readonly ILogger<ChatService> _logger;
    private readonly OpenAIClient? _openAIClient;

    public ChatService(IConfiguration configuration, DatabaseService databaseService, ILogger<ChatService> logger)
    {
        _configuration = configuration;
        _databaseService = databaseService;
        _logger = logger;

        try
        {
            var endpoint = _configuration["OpenAI:Endpoint"];
            var managedIdentityClientId = _configuration["ManagedIdentityClientId"];

            if (!string.IsNullOrEmpty(endpoint))
            {
                var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    ManagedIdentityClientId = managedIdentityClientId
                });
                _openAIClient = new OpenAIClient(new Uri(endpoint), credential);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize OpenAI client");
        }
    }

    public async Task<string> ChatAsync(string userMessage, int currentUserId)
    {
        if (_openAIClient == null)
        {
            return "Chat service is not configured. Please check OpenAI settings.";
        }

        try
        {
            var deploymentName = _configuration["OpenAI:DeploymentName"] ?? "gpt-4";
            
            var chatMessages = new List<ChatRequestMessage>
            {
                new ChatRequestSystemMessage("You are an expense management assistant. Help users manage their expenses, get summaries, and answer questions about their expenses. You have access to functions to query the database."),
                new ChatRequestUserMessage(userMessage)
            };

            var functionDefinitions = GetFunctionDefinitions();
            
            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                DeploymentName = deploymentName,
                Messages = { chatMessages[0], chatMessages[1] },
                Functions = { functionDefinitions[0], functionDefinitions[1], functionDefinitions[2], functionDefinitions[3] },
                FunctionCall = FunctionDefinition.Auto
            };

            var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
            var choice = response.Value.Choices[0];

            if (choice.FinishReason == CompletionsFinishReason.FunctionCall && choice.Message.FunctionCall != null)
            {
                var functionCall = choice.Message.FunctionCall;
                var functionResult = await ExecuteFunctionAsync(functionCall.Name, functionCall.Arguments, currentUserId);
                
                chatMessages.Add(new ChatRequestAssistantMessage(choice.Message.Content) { FunctionCall = functionCall });
                chatMessages.Add(new ChatRequestFunctionMessage(functionCall.Name, functionResult));

                var secondOptions = new ChatCompletionsOptions
                {
                    DeploymentName = deploymentName
                };
                foreach (var msg in chatMessages)
                {
                    secondOptions.Messages.Add(msg);
                }

                var secondResponse = await _openAIClient.GetChatCompletionsAsync(secondOptions);
                return secondResponse.Value.Choices[0].Message.Content;
            }

            return choice.Message.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat service");
            return $"Error processing your request: {ex.Message}";
        }
    }

    private List<FunctionDefinition> GetFunctionDefinitions()
    {
        return new List<FunctionDefinition>
        {
            new FunctionDefinition
            {
                Name = "get_user_expenses",
                Description = "Get all expenses for a specific user",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    type = "object",
                    properties = new
                    {
                        userId = new { type = "integer", description = "The ID of the user" }
                    },
                    required = new[] { "userId" }
                })
            },
            new FunctionDefinition
            {
                Name = "get_expense_summary",
                Description = "Get a summary of all expenses including totals and counts",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    type = "object",
                    properties = new { }
                })
            },
            new FunctionDefinition
            {
                Name = "get_pending_expenses",
                Description = "Get all expenses that are pending approval",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    type = "object",
                    properties = new { }
                })
            },
            new FunctionDefinition
            {
                Name = "get_expenses_by_status",
                Description = "Get expenses filtered by status",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    type = "object",
                    properties = new
                    {
                        statusId = new { type = "integer", description = "The status ID (1=Draft, 2=Pending, 3=Approved, 4=Rejected)" }
                    },
                    required = new[] { "statusId" }
                })
            }
        };
    }

    private async Task<string> ExecuteFunctionAsync(string functionName, string arguments, int currentUserId)
    {
        try
        {
            switch (functionName)
            {
                case "get_user_expenses":
                    var userArgs = JsonSerializer.Deserialize<Dictionary<string, int>>(arguments);
                    var userId = userArgs?["userId"] ?? currentUserId;
                    var userExpenses = await _databaseService.GetExpensesByUserIdAsync(userId);
                    return JsonSerializer.Serialize(new
                    {
                        count = userExpenses.Count,
                        total = userExpenses.Sum(e => e.Amount),
                        expenses = userExpenses.Select(e => new
                        {
                            id = e.ExpenseId,
                            amount = e.Amount,
                            date = e.ExpenseDate,
                            description = e.Description,
                            category = e.CategoryName,
                            status = e.StatusName
                        })
                    });

                case "get_expense_summary":
                    var summary = await _databaseService.GetExpenseSummaryAsync();
                    return JsonSerializer.Serialize(summary);

                case "get_pending_expenses":
                    var pendingExpenses = await _databaseService.GetPendingExpensesAsync();
                    return JsonSerializer.Serialize(new
                    {
                        count = pendingExpenses.Count,
                        total = pendingExpenses.Sum(e => e.Amount),
                        expenses = pendingExpenses.Select(e => new
                        {
                            id = e.ExpenseId,
                            amount = e.Amount,
                            user = e.UserName,
                            description = e.Description,
                            category = e.CategoryName
                        })
                    });

                case "get_expenses_by_status":
                    var statusArgs = JsonSerializer.Deserialize<Dictionary<string, int>>(arguments);
                    var statusId = statusArgs?["statusId"] ?? 2;
                    var statusExpenses = await _databaseService.GetExpensesByStatusAsync(statusId);
                    return JsonSerializer.Serialize(new
                    {
                        count = statusExpenses.Count,
                        total = statusExpenses.Sum(e => e.Amount),
                        expenses = statusExpenses.Select(e => new
                        {
                            id = e.ExpenseId,
                            amount = e.Amount,
                            user = e.UserName,
                            description = e.Description,
                            category = e.CategoryName
                        })
                    });

                default:
                    return JsonSerializer.Serialize(new { error = "Unknown function" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function {FunctionName}", functionName);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}
