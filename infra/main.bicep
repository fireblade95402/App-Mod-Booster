// Main Bicep template for Expense Management System
targetScope = 'resourceGroup'

@description('Location for all resources')
param location string = 'uksouth'

@description('Azure AD admin login (UPN)')
param adminLogin string

@description('Azure AD admin object ID')
param adminObjectId string

@description('Deploy GenAI resources (Azure OpenAI, AI Search)')
param deployGenAI bool = false

// Generate unique names using resource group ID
var uniqueSuffix = uniqueString(resourceGroup().id)
var appServiceName = 'app-expensemgmt-${uniqueSuffix}'
var sqlServerName = 'sql-expensemgmt-${uniqueSuffix}'
var managedIdentityName = 'mid-expensemgmt-${uniqueSuffix}'

// Deploy Managed Identity
module managedIdentity 'modules/managed-identity.bicep' = {
  name: 'managedIdentity-deployment'
  params: {
    location: location
    managedIdentityName: managedIdentityName
  }
}

// Deploy App Service
module appService 'modules/app-service.bicep' = {
  name: 'appService-deployment'
  params: {
    location: location
    appServiceName: appServiceName
    managedIdentityId: managedIdentity.outputs.managedIdentityId
    managedIdentityPrincipalId: managedIdentity.outputs.managedIdentityPrincipalId
  }
}

// Deploy Azure SQL
module azureSQL 'modules/azure-sql.bicep' = {
  name: 'azureSQL-deployment'
  params: {
    location: location
    sqlServerName: sqlServerName
    adminLogin: adminLogin
    adminObjectId: adminObjectId
    managedIdentityPrincipalId: managedIdentity.outputs.managedIdentityPrincipalId
  }
}

// Deploy GenAI resources (conditional)
module genai 'modules/genai.bicep' = if (deployGenAI) {
  name: 'genai-deployment'
  params: {
    location: location
    uniqueSuffix: uniqueSuffix
    managedIdentityPrincipalId: managedIdentity.outputs.managedIdentityPrincipalId
  }
}

// Outputs
output appServiceName string = appService.outputs.appServiceName
output appServiceUrl string = appService.outputs.appServiceUrl
output sqlServerFqdn string = azureSQL.outputs.sqlServerFqdn
output databaseName string = azureSQL.outputs.databaseName
output managedIdentityClientId string = managedIdentity.outputs.managedIdentityClientId
output managedIdentityName string = managedIdentity.outputs.managedIdentityName

// Conditional GenAI outputs
output openAIEndpoint string = deployGenAI ? genai.outputs.openAIEndpoint : ''
output openAIModelName string = deployGenAI ? genai.outputs.openAIModelName : ''
output searchEndpoint string = deployGenAI ? genai.outputs.searchEndpoint : ''
output openAIName string = deployGenAI ? genai.outputs.openAIName : ''
