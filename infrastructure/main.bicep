// Main Bicep template for Expense Management System
// Orchestrates deployment of all Azure resources

@description('Location for all resources')
param location string = resourceGroup().location

@description('Admin Object ID for SQL Server Entra ID authentication')
param adminObjectId string

@description('Admin Login (UPN) for SQL Server Entra ID authentication')
param adminLogin string

@description('Deploy GenAI resources (Azure OpenAI)')
param deployGenAI bool = false

// Generate unique suffix for resource naming
var uniqueSuffix = uniqueString(resourceGroup().id)

// Deploy Managed Identity
module managedIdentity 'managed-identity.bicep' = {
  name: 'managed-identity-deployment'
  params: {
    location: location
    uniqueSuffix: uniqueSuffix
  }
}

// Deploy App Service
module appService 'app-service.bicep' = {
  name: 'app-service-deployment'
  params: {
    location: location
    uniqueSuffix: uniqueSuffix
    managedIdentityId: managedIdentity.outputs.managedIdentityId
  }
}

// Deploy Azure SQL Database
module sqlDatabase 'azure-sql.bicep' = {
  name: 'azure-sql-deployment'
  params: {
    location: location
    uniqueSuffix: uniqueSuffix
    adminObjectId: adminObjectId
    adminLogin: adminLogin
    managedIdentityPrincipalId: managedIdentity.outputs.managedIdentityPrincipalId
  }
}

// Deploy GenAI resources (conditional)
module genai 'genai.bicep' = if (deployGenAI) {
  name: 'genai-deployment'
  params: {
    location: 'swedencentral' // GPT-4o availability
    uniqueSuffix: uniqueSuffix
    managedIdentityPrincipalId: managedIdentity.outputs.managedIdentityPrincipalId
  }
}

// Outputs
output appServiceName string = appService.outputs.appServiceName
output appServiceUrl string = appService.outputs.appServiceUrl
output sqlServerFqdn string = sqlDatabase.outputs.sqlServerFqdn
output sqlDatabaseName string = sqlDatabase.outputs.databaseName
output managedIdentityName string = managedIdentity.outputs.managedIdentityName
output managedIdentityClientId string = managedIdentity.outputs.managedIdentityClientId
output managedIdentityPrincipalId string = managedIdentity.outputs.managedIdentityPrincipalId

// Conditional GenAI outputs
output openAIEndpoint string = deployGenAI ? genai.outputs.openAIEndpoint : ''
output openAIModelName string = deployGenAI ? genai.outputs.openAIModelName : ''
output openAIName string = deployGenAI ? genai.outputs.openAIName : ''
