# Expense Management System

A complete ASP.NET Core 8 Razor Pages + Web API application for managing business expenses with modern UI, Azure integration, and AI-powered chat assistance.

## Features

### Razor Pages (UI)
- **Expenses List** (`/Expenses`) - View and filter all expenses with modern, responsive design
- **Add Expense** (`/Expenses/AddExpense`) - Create new expenses with save as draft or submit for approval
- **Approve Expenses** (`/Expenses/ApproveExpenses`) - Manager view to approve/reject pending expenses

### Web API Endpoints

#### Expenses API (`/api/expenses`)
- `GET /api/expenses` - Get all expenses
- `GET /api/expenses/user/{userId}` - Get expenses by user
- `GET /api/expenses/status/{statusId}` - Get expenses by status
- `GET /api/expenses/{id}` - Get expense by ID
- `GET /api/expenses/pending` - Get pending expenses
- `GET /api/expenses/summary` - Get expense summary statistics
- `POST /api/expenses` - Create new expense
- `POST /api/expenses/{id}/submit` - Submit expense for approval
- `POST /api/expenses/{id}/approve` - Approve expense
- `POST /api/expenses/{id}/reject` - Reject expense
- `PUT /api/expenses/{id}` - Update expense
- `DELETE /api/expenses/{id}` - Delete expense

#### Categories API (`/api/categories`)
- `GET /api/categories` - Get all expense categories
- `GET /api/categories/statuses` - Get all expense statuses

#### Users API (`/api/users`)
- `GET /api/users` - Get all users
- `GET /api/users/{id}` - Get user by ID

#### Chat API (`/api/chat`)
- `POST /api/chat` - AI-powered chat with function calling for expense queries

### Key Technologies

- **ASP.NET Core 8** - Modern web framework
- **Razor Pages** - Server-side rendered UI
- **Web API** - RESTful API endpoints
- **Azure SQL Database** - Cloud database with Managed Identity authentication
- **Azure OpenAI** - AI-powered chat with function calling
- **Swagger/OpenAPI** - API documentation (available at `/swagger`)

## Architecture

### Database Access
- All database operations use **stored procedures only**
- **Managed Identity** authentication for secure, password-free SQL access
- **Error handling** with dummy data fallback for resilience
- Available stored procedures:
  - GetAllExpenses, GetExpensesByUserId, GetExpensesByStatus, GetExpenseById
  - CreateExpense, SubmitExpense, ApproveExpense, RejectExpense, UpdateExpense, DeleteExpense
  - GetAllCategories, GetAllStatuses, GetAllUsers, GetUserById
  - GetPendingExpenses, GetExpenseSummary

### Services

#### DatabaseService
- Handles all SQL database operations via stored procedures
- Uses Azure Managed Identity for authentication
- Includes comprehensive error handling and logging
- Provides dummy data fallback for development/testing

#### ChatService
- Azure OpenAI integration with function calling
- Supports natural language queries about expenses
- Functions available:
  - `get_user_expenses` - Get expenses for a specific user
  - `get_expense_summary` - Get overall expense statistics
  - `get_pending_expenses` - Get all pending approvals
  - `get_expenses_by_status` - Filter expenses by status

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:{server};Database=Northwind;Authentication=Active Directory Managed Identity;User Id={managed-identity-client-id};"
  },
  "OpenAI": {
    "Endpoint": "https://your-openai-resource.openai.azure.com/",
    "DeploymentName": "gpt-4"
  },
  "ManagedIdentityClientId": "your-managed-identity-client-id"
}
```

### Required Configuration
1. **SQL Connection String** - Update with your Azure SQL server details
2. **OpenAI Endpoint** - Your Azure OpenAI resource endpoint
3. **OpenAI Deployment** - Your GPT-4 deployment name
4. **Managed Identity Client ID** - For both SQL and OpenAI authentication

## Building and Running

### Prerequisites
- .NET 8 SDK
- Azure SQL Database with stored procedures deployed
- Azure OpenAI resource (optional for chat feature)
- Managed Identity configured with appropriate permissions

### Build
```bash
cd src/ExpenseManagement
dotnet restore
dotnet build
```

### Run
```bash
dotnet run
```

Access the application:
- **UI**: https://localhost:5001
- **API**: https://localhost:5001/api
- **Swagger**: https://localhost:5001/swagger

## NuGet Packages

- `Microsoft.Data.SqlClient` (5.1.5) - SQL Server connectivity
- `Azure.Identity` (1.11.3) - Managed Identity authentication
- `Swashbuckle.AspNetCore` (6.5.0) - Swagger/OpenAPI support
- `Azure.AI.OpenAI` (1.0.0-beta.15) - Azure OpenAI integration

## Modern UI Design

The UI is inspired by modern expense management applications with:
- Clean, card-based layout
- Responsive design for mobile and desktop
- Color-coded status badges
- Interactive hover effects
- Professional color scheme with accessibility in mind
- SVG icons throughout
- Modern CSS with CSS variables for theming

## Error Handling

The application includes comprehensive error handling:
- Database errors fall back to dummy data
- Detailed error messages in logs
- User-friendly error messages in UI
- API returns appropriate HTTP status codes with error details

## Security

- **Managed Identity** - No passwords stored in code or config
- **Parameterized queries** - All stored procedures use parameters
- **HTTPS** - Enforced in production
- **Input validation** - Model validation on all forms
- **CORS** - Can be configured as needed

## Future Enhancements

- User authentication and authorization
- Receipt file upload support
- Email notifications for approvals
- Budget tracking and alerts
- Advanced reporting and analytics
- Export to Excel/PDF
- Multi-currency support
- Approval workflows with multiple levels

## License

This is a demonstration application for expense management system modernization.
