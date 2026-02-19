# Deployment Order and Considerations

## Overview

This document outlines the recommended deployment order and important considerations when deploying the Expense Management System to Azure.

## Deployment Order

### Basic Deployment (deploy.sh)

1. **Create Resource Group**
   - Creates the Azure Resource Group in UK South

2. **Deploy Infrastructure** (via Bicep)
   - Creates User-Assigned Managed Identity with timestamp-based naming
   - Creates App Service Plan (S1 SKU)
   - Creates App Service with Managed Identity assigned
   - Creates Azure SQL Server with Entra ID authentication
   - Creates Northwind database (Basic tier)
   - Configures firewall rules for Azure services

3. **Configure App Service Settings**
   - Sets connection string with Managed Identity authentication
   - Sets AZURE_CLIENT_ID environment variable
   - **Wait**: 30 seconds for SQL Server to be fully ready

4. **Configure SQL Firewall**
   - Adds current deployment IP address
   - Adds Azure services firewall rule
   - **Wait**: 15 seconds for firewall rules to propagate

5. **Install Python Dependencies**
   - Installs pyodbc and azure-identity packages

6. **Update Configuration Files**
   - Updates Python scripts with actual SQL server FQDN
   - Updates script.sql with actual Managed Identity name

7. **Import Database Schema**
   - Runs run-sql.py to import database_schema.sql
   - Creates tables, inserts sample data

8. **Configure Database Roles**
   - Runs run-sql-dbrole.py to set up Managed Identity permissions
   - Grants db_datareader, db_datawriter, and EXECUTE permissions

9. **Deploy Stored Procedures**
   - Runs run-sql-stored-procs.py
   - Creates all stored procedures for the application

10. **Build and Deploy Application**
    - Builds .NET application in Release mode
    - Creates app.zip with files at root (not in subdirectory)
    - Deploys zip to Azure App Service

### Full Deployment with GenAI (deploy-with-chat.sh)

Follows all steps from Basic Deployment, plus:

1. **Deploy GenAI Resources** (during infrastructure deployment)
   - Creates Azure OpenAI in Sweden Central (regardless of resource group location)
   - Deploys GPT-4o model with capacity 8
   - Creates Azure AI Search (Basic tier)
   - Assigns "Cognitive Services OpenAI User" role to Managed Identity
   - Assigns "Search Index Data Reader" role to Managed Identity

2. **Configure App Service with GenAI Settings**
   - Adds OpenAI__Endpoint configuration
   - Adds OpenAI__DeploymentName configuration
   - Adds ManagedIdentityClientId for authentication

## Important Considerations

### Timing and Waits

- **30-second wait** after infrastructure deployment: Ensures SQL Server is fully provisioned
- **15-second wait** after firewall rules: Allows rules to propagate across Azure infrastructure

### Resource Naming

- Uses `uniqueString(resourceGroup().id)` for deterministic unique names
- Managed Identity uses timestamp format: `mid-appmodassist-DD-HH-MM`
- **Never use** `utcNow()` in Bicep variables (only allowed in parameter defaults)
- All Azure resource names must be lowercase

### API Versions

- Use stable GA API versions (e.g., `@2021-11-01`) not preview versions
- Avoid `@2023-05-01-preview` which lacks complete type definitions

### Bicep Best Practices

- Use `parent` property for child resources instead of concatenated names
- Use null-safe operators (`?.` and `??`) for conditional module outputs
- Pass Managed Identity Principal ID (not resource ID) for role assignments

### Database Connection

- Use Managed Identity authentication only (no SQL authentication)
- Connection string format: `Authentication=Active Directory Managed Identity;User Id={clientId}`
- For local development: Use `Authentication=Active Directory Default` and run `az login`

### GenAI Configuration

- Azure OpenAI must be deployed to Sweden Central for GPT-4o availability
- Use lowercase for customSubDomainName to avoid validation errors
- Configuration happens post-deployment via Azure CLI (not during Bicep deployment)
- This avoids circular dependency between App Service and OpenAI resources

### Security

- SQL Server uses Azure AD-only authentication (SQL auth disabled)
- All services communicate via Managed Identity
- HTTPS only for App Service
- Firewall rules configured for both Azure services and deployment IP

### App.zip Structure

- Files must be at root of zip, not in subdirectory
- Incorrect: `app.zip/publish/ExpenseManagement.dll`
- Correct: `app.zip/ExpenseManagement.dll`
- Created from publish directory using: `cd publish && zip -r ../app.zip .`

### Cross-Platform Compatibility

- Use `sed -i.bak` with explicit backup removal for Mac compatibility
- Example: `sed -i.bak "s/old/new/g" file.txt && rm -f file.txt.bak`

### Cost Optimization

If cost is a concern after initial deployment:
- Scale down App Service from S1 to B1 (~$13/month)
- Keep Azure SQL Basic tier (minimal cost)
- Consider Free tier for Azure OpenAI (with quota limits)
- Azure AI Search Basic tier required for production features

### Rollback Strategy

If deployment fails:
1. Check Azure Portal for detailed error messages
2. Review deployment logs: `az deployment group show`
3. Delete resource group and start fresh if needed: `az group delete --name rg-expensemgmt-demo`
4. Ensure all prerequisites are installed (Azure CLI, Python, ODBC Driver)

### Monitoring Post-Deployment

After successful deployment:
- Check App Service logs for startup errors
- Test database connectivity via /api/users endpoint
- Verify Swagger UI is accessible
- Test chat UI if GenAI was deployed
- Monitor for authentication errors in Application Insights

## Troubleshooting Common Issues

### "Unable to load the proper Managed Identity"
- Ensure AZURE_CLIENT_ID is set in App Service configuration
- Verify Managed Identity has required role assignments

### SQL Connection Timeout
- Check firewall rules include your IP address
- Verify Azure services firewall rule exists (0.0.0.0 to 0.0.0.0)
- Wait longer for SQL Server provisioning (try 60 seconds)

### GenAI Not Working
- Confirm OpenAI__Endpoint and OpenAI__DeploymentName are set
- Check role assignments on OpenAI resource
- Verify model deployment completed successfully

### Bicep Validation Errors
- Use stable API versions (not preview)
- Check all parameters are being passed correctly
- Verify resource types support the specified properties
