// Azure SQL Database Bicep template
// Creates Azure SQL Server and Database with Entra ID-only authentication

@description('Location for the SQL Server')
param location string = resourceGroup().location

@description('Unique suffix for resource naming')
param uniqueSuffix string

@description('Admin Object ID for Entra ID authentication')
param adminObjectId string

@description('Admin Login (UPN) for Entra ID authentication')
param adminLogin string

@description('Managed Identity Principal ID for database access')
param managedIdentityPrincipalId string

// Resource names (lowercase as per requirements)
var sqlServerName = 'sql-expensemgmt-${uniqueSuffix}'
var databaseName = 'Northwind'

// SQL Server with Entra ID-only authentication
resource sqlServer 'Microsoft.Sql/servers@2021-11-01' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: 'sqladmin' // Required but won't be used due to Entra ID-only auth
    administratorLoginPassword: guid(resourceGroup().id, sqlServerName) // Required but won't be used
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// Entra ID Administrator
resource sqlAdministrator 'Microsoft.Sql/servers/administrators@2021-11-01' = {
  parent: sqlServer
  name: 'ActiveDirectory'
  properties: {
    administratorType: 'ActiveDirectory'
    login: adminLogin
    sid: adminObjectId
    tenantId: subscription().tenantId
    azureADOnlyAuthentication: true
  }
}

// SQL Database (Basic tier for development)
resource sqlDatabase 'Microsoft.Sql/servers/databases@2021-11-01' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
    capacity: 5
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648 // 2GB
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: false
    readScale: 'Disabled'
  }
}

// Firewall rule for Azure services
resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
  parent: sqlServer
  name: 'AllowAllAzureIPs'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Outputs
output sqlServerName string = sqlServer.name
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output databaseName string = sqlDatabase.name
