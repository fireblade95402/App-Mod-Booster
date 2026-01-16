![Header image](https://github.com/DougChisholm/App-Mod-Booster/blob/main/repo-header-booster.png)

# App-Mod-Booster

A project to show how GitHub coding agent can turn screenshots of a legacy app into a working proof-of-concept for a cloud native Azure replacement if the legacy database schema is also provided.

## 📚 Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Features](#features)
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Deployment Options](#deployment-options)
- [Local Development](#local-development)
- [API Documentation](#api-documentation)
- [Troubleshooting](#troubleshooting)

## 🎯 Overview

This repository demonstrates the modernization of a legacy **Expense Management System** into a modern Azure cloud-native application. The legacy system included:

- **3 Screenshots** showing the UI (Add Expense, View Expenses, Approve Expenses)
- **SQL Database Schema** with tables for Users, Roles, Expenses, Categories, and Status

The modernized application includes:
- ✅ ASP.NET Core 8.0 Razor Pages web application
- ✅ RESTful APIs with Swagger documentation
- ✅ Azure SQL Database with Entra ID authentication
- ✅ Managed Identity for secure, passwordless connections
- ✅ Stored procedures for all database operations
- ✅ AI-powered chat interface (optional with GenAI deployment)
- ✅ Modern, responsive UI design

## 🏗️ Architecture

See [ARCHITECTURE.md](./ARCHITECTURE.md) for detailed architecture diagrams and data flow.

**Key Components:**
- **App Service**: Hosts the ASP.NET Core application (Linux, S1 SKU)
- **Azure SQL Database**: Stores expense data (Entra ID only auth, Basic tier)
- **Managed Identity**: Provides passwordless authentication
- **Azure OpenAI** (optional): Powers AI chat functionality with GPT-4o

## ✨ Features

### Core Functionality
- 📝 **Add Expense**: Submit expenses with amount, date, category, and description
- 📋 **View Expenses**: List and filter expenses by user, category, and status
- ✅ **Approve Expenses**: Manager workflow to approve/reject pending expenses
- 📊 **Expense Summary**: View statistics and totals

### Technical Features
- 🔐 **Security**: Managed Identity + Entra ID (no passwords/secrets)
- 🗄️ **Database**: All operations via stored procedures (no T-SQL in code)
- 🌐 **APIs**: RESTful APIs with full Swagger/OpenAPI documentation
- 🤖 **AI Chat** (optional): Natural language interface with function calling
- ⚡ **Performance**: S1 App Service plan (no cold start)
- 📱 **Responsive**: Modern, clean UI that works on all devices

## 📋 Prerequisites

- **Azure Subscription** with appropriate permissions
- **Azure CLI** installed and configured
- **Git** for cloning the repository
- **.NET 8 SDK** (for local development)
- **Python 3.x** with pip (for database scripts)

## 🚀 Quick Start

### Steps to Modernise Your Own App

1. **Fork this repo**
2. **Replace the content**:
   - Add your screenshots to `Legacy-Screenshots/`
   - Add your database schema to `Database-Schema/database_schema.sql`
3. **Open the coding agent** and use the app-mod-booster agent, telling it: **"modernise my app"**
4. **Wait for generation** (can take up to 30 minutes) - a pull request will be created
5. **Deploy to Azure**:
   - Use GitHub Codespaces or clone locally
   - Run `az login` to set your Azure context
   - Run `bash deploy.sh` or `bash deploy-with-chat.sh`

### Deploy the Sample Expense Management App

```bash
# Clone the repository
git clone https://github.com/YourUsername/App-Mod-Booster.git
cd App-Mod-Booster

# Login to Azure
az login

# Set your subscription (if you have multiple)
az account set --subscription "Your-Subscription-Name"

# Deploy without GenAI (basic deployment)
bash deploy.sh

# OR Deploy with GenAI (includes AI chat functionality)
bash deploy-with-chat.sh
```

## 🎛️ Deployment Options

### Option 1: Basic Deployment (`deploy.sh`)

**Deploys:**
- ✅ Azure App Service (Linux, S1 SKU)
- ✅ Azure SQL Database (Basic tier)
- ✅ User-Assigned Managed Identity
- ✅ Web application with Razor Pages
- ✅ RESTful APIs

**Chat UI:** Shows a message explaining GenAI is not deployed

**Use Case:** Quick deployment for testing, demos without AI features

**Estimated Cost:** ~$15-20/month for Basic tier SQL + S1 App Service

### Option 2: Full Deployment (`deploy-with-chat.sh`)

**Deploys:**
- ✅ Everything from Option 1
- ✅ Azure OpenAI (GPT-4o model in swedencentral)
- ✅ AI-powered chat interface with function calling

**Chat UI:** Fully functional AI assistant for expense management

**Use Case:** Full-featured deployment with AI capabilities

**Estimated Cost:** ~$30-40/month (includes OpenAI S0 tier)

## 🔧 Deployment Process

Both deployment scripts follow this order:

1. **Create Resource Group** (if not exists)
2. **Deploy Infrastructure** (Bicep templates)
   - Managed Identity
   - App Service Plan & App Service
   - Azure SQL Server & Database
   - Azure OpenAI (if deploy-with-chat.sh)
3. **Configure App Service Settings**
   - ManagedIdentityClientId
   - SqlServer, SqlDatabase
   - OpenAI__Endpoint (if GenAI)
4. **Configure SQL Firewall**
   - Allow Azure services
   - Add deployment IP
5. **Import Database Schema**
   - Run database_schema.sql
   - Create tables and sample data
6. **Configure Database Permissions**
   - Grant managed identity DB access
   - Assign db_datareader, db_datawriter, EXECUTE
7. **Create Stored Procedures**
   - All CRUD operations
8. **Deploy Application**
   - Upload app.zip to App Service

## 💻 Local Development

To run the application locally:

1. **Login to Azure**:
   ```bash
   az login
   ```

2. **Update Connection String**:
   Edit `app/appsettings.json` and change the connection string to:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=tcp:your-sql-server.database.windows.net;Database=Northwind;Authentication=Active Directory Default;"
   }
   ```

3. **Run the Application**:
   ```bash
   cd app
   dotnet run
   ```

4. **Access Locally**:
   - Web App: `https://localhost:5001/Index`
   - Swagger: `https://localhost:5001/swagger`

## 📖 API Documentation

After deployment, access the Swagger documentation at:
```
https://app-expensemgmt-{uniqueSuffix}.azurewebsites.net/swagger
```

### Available Endpoints

**Expenses:**
- `GET /api/expenses` - Get all expenses (with optional filters)
- `GET /api/expenses/{id}` - Get expense by ID
- `GET /api/expenses/pending` - Get pending expenses
- `GET /api/expenses/summary` - Get expense statistics
- `POST /api/expenses` - Create new expense
- `PUT /api/expenses/{id}` - Update expense
- `POST /api/expenses/{id}/submit` - Submit expense for approval
- `POST /api/expenses/{id}/approve` - Approve expense (manager)
- `POST /api/expenses/{id}/reject` - Reject expense (manager)
- `DELETE /api/expenses/{id}` - Delete expense

**Reference Data:**
- `GET /api/categories` - Get all expense categories
- `GET /api/statuses` - Get all expense statuses
- `GET /api/users` - Get all users

**Chat (if GenAI deployed):**
- `GET /api/chat/health` - Check if GenAI is available
- `POST /api/chat/message` - Send message to AI assistant

## 🔍 Troubleshooting

### Common Issues

**1. Database Connection Errors**

If you see "managed identity" errors:
- Ensure the managed identity is assigned to the App Service
- Verify the managed identity has database permissions
- Check the `ManagedIdentityClientId` app setting is correct
- Run `python3 run-sql-dbrole.py` to re-apply permissions

**2. SQL Firewall Issues**

If you can't connect to SQL:
- Verify firewall rules allow Azure services (0.0.0.0)
- Check your deployment IP is whitelisted
- Wait 15-30 seconds for firewall changes to propagate

**3. Chat UI Not Working**

If chat shows errors:
- If using `deploy.sh`, chat will show "GenAI not deployed" message (expected)
- If using `deploy-with-chat.sh`, check:
  - `OpenAI__Endpoint` app setting is set
  - Managed identity has "Cognitive Services OpenAI User" role
  - Azure OpenAI deployment exists in swedencentral

**4. Application Not Starting**

- Check App Service logs in Azure Portal
- Verify `app.zip` deployed correctly
- Ensure all app settings are configured

### Getting Help

1. Check the **ARCHITECTURE.md** for system design
2. Review **Azure Portal** logs for the App Service
3. Use **Application Insights** (if configured) for detailed telemetry
4. Check the **GitHub Issues** for similar problems

## 📝 Notes

- **URL Format**: Access the app at `https://your-app.azurewebsites.net/Index` (not root `/`)
- **Database Name**: The database is called "Northwind" per the schema requirements
- **Stored Procedures**: All database operations use stored procedures (no direct T-SQL)
- **Resource Naming**: All Azure resources use lowercase names with `uniqueString()` for uniqueness
- **Security**: No SQL authentication - only Entra ID (Azure AD) authentication

## 📚 Supporting Materials

For Microsoft Employees:
[Presentation Slides](https://microsofteur-my.sharepoint.com/:p:/g/personal/dchisholm_microsoft_com/IQAY41LQ12fjSIfFz3ha4hfFAZc7JQQuWaOrF7ObgxRK6f4?e=p6arJs)

## 🤝 Contributing

This is a demonstration project. To contribute:
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## 📄 License

See the LICENSE file for details.

---

**Built with ❤️ using GitHub Copilot and Azure**

