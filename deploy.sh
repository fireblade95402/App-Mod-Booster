#!/bin/bash
set -e

# Expense Management System Deployment Script (without GenAI)
# This script deploys the infrastructure and application to Azure

echo "======================================"
echo "Expense Management System Deployment"
echo "======================================"

# Configuration
RESOURCE_GROUP="${RESOURCE_GROUP:-rg-expensemgmt-demo}"
LOCATION="${LOCATION:-uksouth}"
DEPLOYMENT_NAME="expensemgmt-deployment-$(date +%s)"

# Get current user's Object ID and UPN for SQL Server admin
echo "Getting current user information..."
ADMIN_OBJECT_ID=$(az ad signed-in-user show --query id -o tsv)
ADMIN_UPN=$(az ad signed-in-user show --query userPrincipalName -o tsv)

echo "Admin Object ID: $ADMIN_OBJECT_ID"
echo "Admin UPN: $ADMIN_UPN"

# Create resource group if it doesn't exist
echo ""
echo "Creating resource group..."
az group create --name $RESOURCE_GROUP --location $LOCATION --output none

# Deploy infrastructure (App Service, SQL Database, Managed Identity)
echo ""
echo "Deploying Azure infrastructure..."
DEPLOYMENT_OUTPUT=$(az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --name $DEPLOYMENT_NAME \
  --template-file infrastructure/main.bicep \
  --parameters adminObjectId=$ADMIN_OBJECT_ID \
  --parameters adminLogin=$ADMIN_UPN \
  --parameters deployGenAI=false \
  --query 'properties.outputs' \
  --output json)

echo "Infrastructure deployed successfully!"

# Extract outputs
APP_SERVICE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceName.value')
APP_SERVICE_URL=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceUrl.value')
SQL_SERVER_FQDN=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlServerFqdn.value')
SQL_DATABASE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlDatabaseName.value')
MANAGED_IDENTITY_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityName.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityClientId.value')

echo ""
echo "Deployment Outputs:"
echo "  App Service: $APP_SERVICE_NAME"
echo "  App URL: $APP_SERVICE_URL"
echo "  SQL Server: $SQL_SERVER_FQDN"
echo "  Database: $SQL_DATABASE_NAME"
echo "  Managed Identity: $MANAGED_IDENTITY_NAME"
echo "  MI Client ID: $MANAGED_IDENTITY_CLIENT_ID"

# Configure App Service settings
echo ""
echo "Configuring App Service settings..."
az webapp config appsettings set \
  --resource-group $RESOURCE_GROUP \
  --name $APP_SERVICE_NAME \
  --settings \
    "ManagedIdentityClientId=$MANAGED_IDENTITY_CLIENT_ID" \
    "SqlServer=$SQL_SERVER_FQDN" \
    "SqlDatabase=$SQL_DATABASE_NAME" \
    "AZURE_CLIENT_ID=$MANAGED_IDENTITY_CLIENT_ID" \
  --output none

echo "App Service settings configured!"

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
  --output none 2>/dev/null || echo "Firewall rule already exists"

# Add deployment IP
echo "Adding deployment IP ($MY_IP) to SQL firewall..."
az sql server firewall-rule create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER_NAME \
  --name "AllowDeploymentIP" \
  --start-ip-address $MY_IP \
  --end-ip-address $MY_IP \
  --output none 2>/dev/null || echo "Firewall rule already exists"

echo "Waiting 15 seconds for firewall rules to propagate..."
sleep 15

# Install Python dependencies
echo ""
echo "Installing Python dependencies..."
pip3 install --quiet pyodbc azure-identity

# Update Python scripts with actual server/database names
echo ""
echo "Updating Python scripts with server and database names..."

# Update run-sql.py
sed -i.bak "s/SERVER = \".*\"/SERVER = \"$SQL_SERVER_FQDN\"/g" run-sql.py && rm -f run-sql.py.bak
sed -i.bak "s/DATABASE = \".*\"/DATABASE = \"$SQL_DATABASE_NAME\"/g" run-sql.py && rm -f run-sql.py.bak

# Update run-sql-dbrole.py
sed -i.bak "s/SERVER = \".*\"/SERVER = \"$SQL_SERVER_FQDN\"/g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak
sed -i.bak "s/DATABASE = \".*\"/DATABASE = \"$SQL_DATABASE_NAME\"/g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak

# Update run-sql-stored-procs.py
sed -i.bak "s/SERVER = \".*\"/SERVER = \"$SQL_SERVER_FQDN\"/g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak
sed -i.bak "s/DATABASE = \".*\"/DATABASE = \"$SQL_DATABASE_NAME\"/g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak

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

# Create stored procedures
echo ""
echo "Creating stored procedures..."
python3 run-sql-stored-procs.py

# Deploy application
echo ""
echo "Deploying application to App Service..."
az webapp deploy \
  --resource-group $RESOURCE_GROUP \
  --name $APP_SERVICE_NAME \
  --src-path ./app.zip \
  --type zip \
  --output none

echo "Application deployed successfully!"

# Final output
echo ""
echo "======================================"
echo "Deployment Complete!"
echo "======================================"
echo ""
echo "Application URL: $APP_SERVICE_URL/Index"
echo "Swagger API Docs: $APP_SERVICE_URL/swagger"
echo "Chat UI: $APP_SERVICE_URL/chatui/index.html"
echo ""
echo "⚠️  Note: The Chat UI will show a message that GenAI services are not deployed."
echo "    To deploy with GenAI, use deploy-with-chat.sh instead."
echo ""
echo "🔐 To run the app locally:"
echo "    1. Run: az login"
echo "    2. Update appsettings.json connection string to use 'Authentication=Active Directory Default'"
echo "    3. Run: dotnet run --project app/ExpenseManagement.csproj"
echo ""
