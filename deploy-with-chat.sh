#!/bin/bash
set -e

# Expense Management System - Full Deployment Script with GenAI
# This script deploys all infrastructure including Azure OpenAI and AI Search

echo "==================================================================="
echo "Expense Management System - Full Deployment with GenAI"
echo "==================================================================="

# Variables
RESOURCE_GROUP="rg-expensemgmt-demo"
LOCATION="uksouth"

# Get current user information for Azure AD admin
echo "Getting current user information..."
ADMIN_USER_ID=$(az ad signed-in-user show --query id -o tsv)
ADMIN_USER_UPN=$(az ad signed-in-user show --query userPrincipalName -o tsv)

echo "Admin User: $ADMIN_USER_UPN"
echo "Admin Object ID: $ADMIN_USER_ID"

# Create resource group
echo ""
echo "Creating resource group..."
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION \
  --output none

# Deploy infrastructure (WITH GenAI)
echo ""
echo "Deploying infrastructure with GenAI services..."
DEPLOYMENT_OUTPUT=$(az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file infra/main.bicep \
  --parameters location=$LOCATION \
               adminLogin="$ADMIN_USER_UPN" \
               adminObjectId="$ADMIN_USER_ID" \
               deployGenAI=true \
  --query 'properties.outputs' \
  --output json)

# Extract outputs
APP_SERVICE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceName.value')
APP_SERVICE_URL=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceUrl.value')
SQL_SERVER_FQDN=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlServerFqdn.value')
DATABASE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.databaseName.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityClientId.value')
MANAGED_IDENTITY_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityName.value')
OPENAI_ENDPOINT=$(echo $DEPLOYMENT_OUTPUT | jq -r '.openAIEndpoint.value')
OPENAI_MODEL_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.openAIModelName.value')
SEARCH_ENDPOINT=$(echo $DEPLOYMENT_OUTPUT | jq -r '.searchEndpoint.value')
OPENAI_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.openAIName.value')

echo ""
echo "Deployment outputs:"
echo "  App Service: $APP_SERVICE_NAME"
echo "  SQL Server: $SQL_SERVER_FQDN"
echo "  Database: $DATABASE_NAME"
echo "  Managed Identity: $MANAGED_IDENTITY_NAME"
echo "  Client ID: $MANAGED_IDENTITY_CLIENT_ID"
echo "  OpenAI Endpoint: $OPENAI_ENDPOINT"
echo "  OpenAI Model: $OPENAI_MODEL_NAME"
echo "  Search Endpoint: $SEARCH_ENDPOINT"

# Configure App Service settings (including GenAI settings)
echo ""
echo "Configuring App Service settings with GenAI..."
CONNECTION_STRING="Server=tcp:$SQL_SERVER_FQDN;Database=$DATABASE_NAME;Authentication=Active Directory Managed Identity;User Id=$MANAGED_IDENTITY_CLIENT_ID;"

az webapp config appsettings set \
  --resource-group $RESOURCE_GROUP \
  --name $APP_SERVICE_NAME \
  --settings \
    "ConnectionStrings__DefaultConnection=$CONNECTION_STRING" \
    "AZURE_CLIENT_ID=$MANAGED_IDENTITY_CLIENT_ID" \
    "ManagedIdentityClientId=$MANAGED_IDENTITY_CLIENT_ID" \
    "OpenAI__Endpoint=$OPENAI_ENDPOINT" \
    "OpenAI__DeploymentName=$OPENAI_MODEL_NAME" \
    "Search__Endpoint=$SEARCH_ENDPOINT" \
  --output none

# Wait for SQL Server to be fully ready
echo ""
echo "Waiting 30 seconds for SQL Server to be fully ready..."
sleep 30

# Add current IP to SQL firewall
echo ""
echo "Adding current IP to SQL firewall..."
MY_IP=$(curl -s https://api.ipify.org)
SQL_SERVER_NAME=$(echo $SQL_SERVER_FQDN | cut -d'.' -f1)

# Allow Azure services access
echo "Allowing Azure services access to SQL Server..."
az sql server firewall-rule create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER_NAME \
  --name "AllowAllAzureIPs" \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0 \
  --output none 2>/dev/null || echo "Azure services rule already exists"

# Add deployment IP
az sql server firewall-rule create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER_NAME \
  --name "AllowDeploymentIP" \
  --start-ip-address $MY_IP \
  --end-ip-address $MY_IP \
  --output none 2>/dev/null || echo "Deployment IP rule already exists"

echo "Waiting additional 15 seconds for firewall rules to propagate..."
sleep 15

# Install required Python packages if not already installed
echo ""
echo "Installing Python dependencies..."
pip3 install --quiet pyodbc azure-identity

# Update Python scripts with actual server name
echo ""
echo "Updating Python scripts with server information..."
sed -i.bak "s/sql-expensemgmt-placeholder.database.windows.net/$SQL_SERVER_FQDN/g" run-sql.py && rm -f run-sql.py.bak
sed -i.bak "s/sql-expensemgmt-placeholder.database.windows.net/$SQL_SERVER_FQDN/g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak
sed -i.bak "s/sql-expensemgmt-placeholder.database.windows.net/$SQL_SERVER_FQDN/g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak

# Update script.sql with managed identity name
sed -i.bak "s/MANAGED-IDENTITY-NAME/$MANAGED_IDENTITY_NAME/g" script.sql && rm -f script.sql.bak

# Import database schema
echo ""
echo "Importing database schema..."
python3 run-sql.py

# Configure database roles for managed identity
echo ""
echo "Configuring database roles for managed identity..."
python3 run-sql-dbrole.py

# Deploy stored procedures
echo ""
echo "Deploying stored procedures..."
python3 run-sql-stored-procs.py

# Build and publish the application
echo ""
echo "Building application..."
cd src/ExpenseManagement
dotnet publish -c Release -o ../../publish

# Create deployment package
echo ""
echo "Creating deployment package..."
cd ../../publish
zip -r ../app.zip . > /dev/null
cd ..

# Deploy application to App Service
echo ""
echo "Deploying application to Azure..."
az webapp deploy \
  --resource-group $RESOURCE_GROUP \
  --name $APP_SERVICE_NAME \
  --src-path ./app.zip \
  --type zip \
  --async true

echo ""
echo "==================================================================="
echo "Deployment Complete!"
echo "==================================================================="
echo ""
echo "📱 Application URL: $APP_SERVICE_URL/Expenses"
echo "   (Note: Navigate to /Expenses to view the app)"
echo ""
echo "📚 API Documentation: $APP_SERVICE_URL/swagger"
echo ""
echo "💬 Chat Interface: $APP_SERVICE_URL/Chat"
echo "   (Powered by Azure OpenAI GPT-4o)"
echo ""
echo "🗄️  Database: $SQL_SERVER_FQDN/$DATABASE_NAME"
echo ""
echo "🤖 Azure OpenAI: $OPENAI_NAME (swedencentral)"
echo "   Model: $OPENAI_MODEL_NAME"
echo ""
echo "⏱️  Note: Application deployment is asynchronous. Wait 2-3 minutes before accessing."
echo ""
echo "==================================================================="
echo ""
echo "🚀 Quick Test Commands:"
echo ""
echo "# List all expenses via API"
echo "curl $APP_SERVICE_URL/api/expenses"
echo ""
echo "# Chat with AI assistant"
echo "curl -X POST $APP_SERVICE_URL/api/chat -H 'Content-Type: application/json' \\"
echo "  -d '{\"message\": \"Show me all pending expenses\"}'"
echo ""
echo "==================================================================="
