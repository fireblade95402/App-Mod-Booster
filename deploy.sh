#!/bin/bash
set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Expense Management System Deployment${NC}"
echo -e "${GREEN}========================================${NC}"

# Variables
RESOURCE_GROUP="rg-expensemgmt-demo"
LOCATION="uksouth"
DEPLOY_GENAI=false

# Get current timestamp for managed identity
TIMESTAMP=$(date +"%d-%H-%M")

# Get current user info for SQL admin
CURRENT_USER=$(az account show --query user.name -o tsv)
CURRENT_USER_OBJECT_ID=$(az ad signed-in-user show --query id -o tsv)

echo -e "${YELLOW}Using current user as SQL Admin:${NC}"
echo "  Login: $CURRENT_USER"
echo "  Object ID: $CURRENT_USER_OBJECT_ID"
echo ""

# Create resource group
echo -e "${GREEN}Creating resource group...${NC}"
az group create --name $RESOURCE_GROUP --location $LOCATION --output none

# Deploy infrastructure
echo -e "${GREEN}Deploying infrastructure...${NC}"
DEPLOYMENT_OUTPUT=$(az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file infra/main.bicep \
  --parameters location=$LOCATION \
  --parameters deployGenAI=$DEPLOY_GENAI \
  --parameters adminObjectId=$CURRENT_USER_OBJECT_ID \
  --parameters adminLogin=$CURRENT_USER \
  --parameters timestamp=$TIMESTAMP \
  --output json)

# Extract outputs
APP_SERVICE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.appServiceName.value')
APP_SERVICE_URL=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.appServiceUrl.value')
MANAGED_IDENTITY_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.managedIdentityName.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.managedIdentityClientId.value')
SQL_SERVER_FQDN=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.sqlServerFqdn.value')
DATABASE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.databaseName.value')

echo -e "${GREEN}Infrastructure deployed successfully!${NC}"
echo "  App Service: $APP_SERVICE_NAME"
echo "  SQL Server: $SQL_SERVER_FQDN"
echo "  Database: $DATABASE_NAME"
echo "  Managed Identity: $MANAGED_IDENTITY_NAME"
echo ""

# Configure App Service settings
echo -e "${GREEN}Configuring App Service settings...${NC}"
az webapp config appsettings set \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    "ConnectionStrings__DefaultConnection=Server=tcp:${SQL_SERVER_FQDN},1433;Database=${DATABASE_NAME};Authentication=Active Directory Managed Identity;User Id=${MANAGED_IDENTITY_CLIENT_ID};" \
  --output none

echo -e "${GREEN}Waiting 30 seconds for SQL Server to be fully ready...${NC}"
sleep 30

# Add current IP to SQL firewall
echo -e "${GREEN}Adding current IP to SQL firewall...${NC}"
MY_IP=$(curl -s https://api.ipify.org)
SQL_SERVER_NAME=$(echo $SQL_SERVER_FQDN | cut -d'.' -f1)

# Allow Azure services access
echo "  Allowing Azure services access to SQL Server..."
az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name "AllowAllAzureIPs" \
    --start-ip-address 0.0.0.0 \
    --end-ip-address 0.0.0.0 \
    --output none

# Add deployment IP
echo "  Adding deployment IP ($MY_IP)..."
az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name "AllowDeploymentIP" \
    --start-ip-address $MY_IP \
    --end-ip-address $MY_IP \
    --output none

echo "  Waiting additional 15 seconds for firewall rules to propagate..."
sleep 15

# Install required Python packages
echo -e "${GREEN}Installing Python dependencies...${NC}"
pip3 install --quiet pyodbc azure-identity

# Update Python scripts with actual server names
echo -e "${GREEN}Updating Python scripts with server details...${NC}"
sed -i.bak "s/sql-expensemgmt-placeholder.database.windows.net/${SQL_SERVER_FQDN}/g" run-sql.py && rm -f run-sql.py.bak
sed -i.bak "s/sql-expensemgmt-placeholder.database.windows.net/${SQL_SERVER_FQDN}/g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak
sed -i.bak "s/sql-expensemgmt-placeholder.database.windows.net/${SQL_SERVER_FQDN}/g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak

# Update script.sql with managed identity name
sed -i.bak "s/MANAGED-IDENTITY-NAME/${MANAGED_IDENTITY_NAME}/g" script.sql && rm -f script.sql.bak

# Import database schema
echo -e "${GREEN}Importing database schema...${NC}"
python3 run-sql.py

# Configure database roles for managed identity
echo -e "${GREEN}Configuring database roles for managed identity...${NC}"
python3 run-sql-dbrole.py

# Deploy stored procedures
echo -e "${GREEN}Deploying stored procedures...${NC}"
python3 run-sql-stored-procs.py

# Build and deploy application
echo -e "${GREEN}Building application...${NC}"
cd app
dotnet publish -c Release -o ./publish

# Create app.zip from publish directory (files at root, not in subdirectory)
echo -e "${GREEN}Creating deployment package...${NC}"
cd publish
zip -r ../app.zip . > /dev/null
cd ..

# Deploy to Azure
echo -e "${GREEN}Deploying application to Azure...${NC}"
az webapp deploy \
  --resource-group $RESOURCE_GROUP \
  --name $APP_SERVICE_NAME \
  --src-path ./app.zip \
  --type zip \
  --output none

cd ..

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Deployment Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${YELLOW}Application URL:${NC} ${APP_SERVICE_URL}/Index"
echo ""
echo -e "${YELLOW}To run locally:${NC}"
echo "  1. Update appsettings.json connection string to use 'Authentication=Active Directory Default'"
echo "  2. Run 'az login' to authenticate"
echo "  3. Run 'dotnet run' in the app directory"
echo ""
