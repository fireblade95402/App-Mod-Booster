![Header image](https://github.com/DougChisholm/App-Mod-Booster/blob/main/repo-header-booster.png)

# App-Mod-Booster
A project to show how GitHub coding agent can turn screenshots of a legacy app into a working proof-of-concept for a cloud native Azure replacement if the legacy database schema is also provided.

Steps to modernise an app:

1. Fork this repo 
2. In new repo replace the screenshots and sql schema (or keep the samples)
3. Open the coding agent and use app-mod-booster agent telling it "modernise my app"
4. When the app code is generated (can take up to 30 minutes) there will be a pull request to approve.
5. Now you can use codespaces to deploy the app to azure (or open VS Code and clone the repo locally - you will need to install some tools locally or use the devcontainer)
6. Open terminal and type "az login" to set subscription/context
7. Then type "bash deploy.sh" to deploy the app and db or "bash deploy-with-chat.sh" to deploy the app, db and chat UI.

Supporting slides for Microsoft Employees:
[Here](<https://microsofteur-my.sharepoint.com/:p:/g/personal/dchisholm_microsoft_com/IQAY41LQ12fjSIfFz3ha4hfFAZc7JQQuWaOrF7ObgxRK6f4?e=p6arJs>)

---

## Generated Application Details

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Azure Cloud                             │
│                                                              │
│  ┌──────────────┐                                           │
│  │  App Service │                                           │
│  │  (ASP.NET 8) │                                           │
│  │              │                                           │
│  │  - Razor UI  │                                           │
│  │  - REST APIs │                                           │
│  │  - Chat UI   │                                           │
│  └──────┬───────┘                                           │
│         │                                                    │
│         │ uses                                               │
│         │                                                    │
│  ┌──────▼──────────────┐                                    │
│  │ User-Assigned       │                                    │
│  │ Managed Identity    │                                    │
│  │                     │                                    │
│  │ Authenticates to:   │                                    │
│  └─────┬───────┬───────┘                                    │
│        │       │                                             │
│        │       └─────────────────┐                          │
│        │                         │                          │
│  ┌─────▼─────────┐      ┌────────▼─────────┐              │
│  │  Azure SQL    │      │   Azure OpenAI   │              │
│  │  Database     │      │                  │              │
│  │               │      │  - GPT-4o model  │              │
│  │  - Northwind  │      │  - Function      │              │
│  │  - Entra Auth │      │    calling       │              │
│  │  - Stored     │      └──────────────────┘              │
│  │    Procedures │                                         │
│  └───────────────┘      ┌──────────────────┐              │
│                         │  Azure AI Search │              │
│                         │                  │              │
│                         │  (for RAG)       │              │
│                         └──────────────────┘              │
└─────────────────────────────────────────────────────────────┘
```

### Features

- ✅ **Modern UI**: Clean, responsive interface using Bootstrap 5
- ✅ **RESTful APIs**: Full CRUD operations with Swagger documentation
- ✅ **AI Chat Assistant**: Natural language interface powered by Azure OpenAI
- ✅ **Secure Authentication**: Azure AD-only authentication using Managed Identity
- ✅ **Database Best Practices**: All data access through stored procedures
- ✅ **Error Handling**: Comprehensive error messages with fallback to dummy data
- ✅ **Infrastructure as Code**: Complete Bicep templates for Azure deployment

### Technology Stack

- **Frontend**: ASP.NET Core Razor Pages, Bootstrap 5, JavaScript
- **Backend**: ASP.NET Core 8.0 Web API
- **Database**: Azure SQL Database with stored procedures
- **Authentication**: Azure Managed Identity with Entra ID
- **AI**: Azure OpenAI (GPT-4o) with function calling
- **Search**: Azure AI Search
- **IaC**: Azure Bicep
- **Deployment**: Azure CLI, Shell scripts

### Accessing the Application

After deployment completes:

- **Main UI**: `https://<app-name>.azurewebsites.net/Index`
- **Chat UI**: `https://<app-name>.azurewebsites.net/Chat`
- **API Docs**: `https://<app-name>.azurewebsites.net/swagger`

### API Endpoints

**Expenses:**
- `GET /api/expenses` - Get all expenses (optional: ?status=Submitted&userId=1)
- `GET /api/expenses/{id}` - Get expense by ID
- `POST /api/expenses` - Create new expense
- `PUT /api/expenses/{id}` - Update expense
- `POST /api/expenses/{id}/submit` - Submit for approval
- `POST /api/expenses/{id}/approve?reviewerId=2` - Approve expense
- `POST /api/expenses/{id}/reject?reviewerId=2` - Reject expense
- `DELETE /api/expenses/{id}` - Delete draft expense

**Lookups:**
- `GET /api/categories` - Get expense categories
- `GET /api/users` - Get all users
- `GET /api/statuses` - Get expense statuses

**Chat:**
- `POST /api/chat` - Send message to AI assistant

### Security Features

- ✅ Azure AD-only authentication (SQL authentication disabled)
- ✅ Managed Identity for all Azure service connections
- ✅ No connection strings with passwords
- ✅ HTTPS only
- ✅ Firewall rules for Azure services
- ✅ Role-based database access

### Troubleshooting

**Database Connection Issues:**
- Check that your IP is in the SQL firewall rules
- Verify the managed identity has db_datareader, db_datawriter, and EXECUTE permissions
- Review the detailed error message in the red banner at the top of the page

**Chat Not Working:**
- Ensure you deployed with `deploy-with-chat.sh`
- Check App Service configuration has `OpenAI__Endpoint` and `OpenAI__DeploymentName`
- Verify managed identity has "Cognitive Services OpenAI User" role

### Cost Optimization

- App Service: S1 SKU (~$70/month) - can be scaled down to B1 after testing
- Azure SQL: Basic tier (~$5/month)
- Azure OpenAI: Pay-per-token (GPT-4o)
- Azure AI Search: Basic tier (~$75/month)

**Total estimated cost**: ~$150-200/month for demo environment
