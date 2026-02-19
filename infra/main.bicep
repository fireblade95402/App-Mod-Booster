@description('Location for all resources')
param location string = 'uksouth'

@description('Whether to deploy GenAI resources')
param deployGenAI bool = false

@description('Entra ID Administrator Object ID')
param adminObjectId string

@description('Entra ID Administrator Login')
param adminLogin string

@description('Timestamp for managed identity name')
param timestamp string

var uniqueSuffix = uniqueString(resourceGroup().id)

// Deploy App Service with Managed Identity
module appService 'app-service.bicep' = {
  name: 'appService-deployment'
  params: {
    location: location
    uniqueSuffix: uniqueSuffix
    timestamp: timestamp
  }
}

// Deploy Azure SQL Database
module azureSQL 'azure-sql.bicep' = {
  name: 'azureSQL-deployment'
  params: {
    location: location
    uniqueSuffix: uniqueSuffix
    adminObjectId: adminObjectId
    adminLogin: adminLogin
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
  }
}

// Conditionally deploy GenAI resources
module genAI 'genai.bicep' = if (deployGenAI) {
  name: 'genAI-deployment'
  params: {
    location: location
    uniqueSuffix: uniqueSuffix
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
  }
}

// Outputs
output appServiceName string = appService.outputs.appServiceName
output appServiceUrl string = appService.outputs.appServiceUrl
output managedIdentityId string = appService.outputs.managedIdentityId
output managedIdentityClientId string = appService.outputs.managedIdentityClientId
output managedIdentityPrincipalId string = appService.outputs.managedIdentityPrincipalId
output managedIdentityName string = appService.outputs.managedIdentityName
output sqlServerName string = azureSQL.outputs.sqlServerName
output sqlServerFqdn string = azureSQL.outputs.sqlServerFqdn
output databaseName string = azureSQL.outputs.databaseName

// Conditional GenAI outputs
output openAIEndpoint string = deployGenAI ? genAI.outputs.openAIEndpoint : ''
output openAIName string = deployGenAI ? genAI.outputs.openAIName : ''
output openAIModelName string = deployGenAI ? genAI.outputs.openAIModelName : ''
output searchEndpoint string = deployGenAI ? genAI.outputs.searchEndpoint : ''
output searchName string = deployGenAI ? genAI.outputs.searchName : ''
