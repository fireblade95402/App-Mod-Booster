# Azure Services Architecture Diagram

## Expense Management System Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         INTERNET / USERS                                │
└───────────────────────────────────┬─────────────────────────────────────┘
                                    │
                                    │ HTTPS
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                      AZURE APP SERVICE (Linux)                          │
│                                                                          │
│  ┌──────────────────────────────────────────────────────────────┐      │
│  │  ASP.NET Core 8.0 Web Application                            │      │
│  │  ┌───────────────┐  ┌───────────────┐  ┌──────────────────┐ │      │
│  │  │ Razor Pages   │  │  Web APIs     │  │  Chat UI (HTML)  │ │      │
│  │  │  - Index      │  │  - Expenses   │  │  - AI Assistant  │ │      │
│  │  │  - Add Exp    │  │  - Categories │  │  - Function Call │ │      │
│  │  │  - View Exp   │  │  - Users      │  │                  │ │      │
│  │  │  - Approve    │  │  - Chat API   │  │                  │ │      │
│  │  └───────────────┘  └───────────────┘  └──────────────────┘ │      │
│  │                                                               │      │
│  │  Services Layer:                                              │      │
│  │  ┌────────────────────┐  ┌──────────────────────────────┐   │      │
│  │  │  ExpenseService    │  │  ChatService (Optional)      │   │      │
│  │  │  - All DB ops via  │  │  - OpenAI Integration        │   │      │
│  │  │    Stored Procs    │  │  - Function Calling          │   │      │
│  │  └────────────────────┘  └──────────────────────────────┘   │      │
│  └──────────────────────────────────────────────────────────────┘      │
│                                                                          │
│  Identity: User-Assigned Managed Identity                               │
│  Settings: ManagedIdentityClientId, SqlServer, SqlDatabase              │
└───────────┬─────────────────┬────────────────────────────────┬─────────┘
            │                 │                                 │
            │ Managed         │ Managed                         │ Managed
            │ Identity        │ Identity                        │ Identity
            │ Auth            │ Auth                            │ Auth
            ▼                 ▼                                 ▼
┌──────────────────┐  ┌───────────────────────┐  ┌────────────────────────┐
│  AZURE SQL DB    │  │  AZURE OPENAI         │  │  USER-ASSIGNED         │
│                  │  │  (Optional - GenAI)   │  │  MANAGED IDENTITY      │
│  ┌────────────┐  │  │                       │  │                        │
│  │ Northwind  │  │  │  Model: GPT-4o        │  │  Created at deploy     │
│  │  Database  │  │  │  Location:            │  │  time with timestamp   │
│  │            │  │  │   swedencentral       │  │                        │
│  │ Tables:    │  │  │                       │  │  Assigned to:          │
│  │ - Users    │  │  │  Deployment: gpt-4o   │  │  - App Service         │
│  │ - Roles    │  │  │  Capacity: 8          │  │  - Azure SQL (DB user) │
│  │ - Expenses │  │  │                       │  │  - Azure OpenAI        │
│  │ - Categori │  │  │  Role Assignments:    │  │                        │
│  │ - Statuses │  │  │  - Cognitive Services │  │  Permissions:          │
│  │            │  │  │    OpenAI User        │  │  - db_datareader       │
│  │ Stored     │  │  │                       │  │  - db_datawriter       │
│  │ Procedures │  │  └───────────────────────┘  │  - EXECUTE             │
│  │ (All CRUD) │  │                              │                        │
│  └────────────┘  │                              └────────────────────────┘
│                  │
│  Authentication: │
│  - Entra ID Only │
│  - No SQL Auth   │
│                  │
│  Firewall:       │
│  - Azure Services│
│  - Deployment IP │
└──────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                         DATA FLOW                                        │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  1. User Request → App Service (HTTPS)                                  │
│  2. App Service → Managed Identity → Azure SQL (Entra ID Auth)          │
│  3. App Service → Stored Procedure Execution → SQL Database             │
│  4. SQL Database → Results → App Service → User (JSON/HTML)             │
│                                                                          │
│  Chat Flow (if GenAI deployed):                                         │
│  5. User Chat Message → ChatController → ChatService                    │
│  6. ChatService → Managed Identity → Azure OpenAI (Token Auth)          │
│  7. Azure OpenAI → Function Calls → ChatService                         │
│  8. ChatService → ExpenseService → Stored Procedures → SQL Database     │
│  9. SQL Results → ChatService → Azure OpenAI → Natural Language         │
│  10. ChatService → User (Formatted HTML Response)                       │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                       SECURITY FEATURES                                  │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  • Managed Identity Authentication (No secrets in code)                 │
│  • Entra ID Only Authentication for SQL (No SQL passwords)              │
│  • HTTPS Only for App Service                                           │
│  • Stored Procedures (No direct SQL injection risk)                     │
│  • Role-Based Access Control (RBAC) for Azure OpenAI                    │
│  • SQL Firewall Rules (Azure Services + Deployment IP only)             │
│  • TLS 1.2 Minimum for all connections                                  │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                     DEPLOYMENT OPTIONS                                   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  Option 1: deploy.sh                                                    │
│  └─ Deploys: App Service + SQL Database + Managed Identity              │
│     Chat UI: Shows "GenAI not deployed" message                         │
│                                                                          │
│  Option 2: deploy-with-chat.sh                                          │
│  └─ Deploys: Everything from Option 1 + Azure OpenAI                    │
│     Chat UI: Fully functional with AI-powered responses                 │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘

## Resource Naming Convention

All resources use lowercase names with uniqueString() for uniqueness:
- Managed Identity: mid-expensemgmt-{timestamp}-{uniqueSuffix}
- App Service Plan: asp-expensemgmt-{uniqueSuffix}
- App Service: app-expensemgmt-{uniqueSuffix}
- SQL Server: sql-expensemgmt-{uniqueSuffix}
- Azure OpenAI: aoai-expensemgmt-{uniqueSuffix}

## Endpoints

- Web Application: https://app-expensemgmt-{uniqueSuffix}.azurewebsites.net/Index
- API Documentation: https://app-expensemgmt-{uniqueSuffix}.azurewebsites.net/swagger
- Chat UI: https://app-expensemgmt-{uniqueSuffix}.azurewebsites.net/chatui/index.html
