// Managed Identity Bicep template
// Creates a user-assigned managed identity for the Expense Management System

@description('Location for the managed identity')
param location string = resourceGroup().location

@description('Unique suffix for resource naming')
param uniqueSuffix string

// Create timestamp-based name component
// Note: Per prompt-017 requirements, managed identity uses timestamp for uniqueness.
// This creates a new identity per deployment which is intentional for demo purposes.
// For production, consider using only uniqueSuffix to reuse the same identity.
var timestamp = utcNow('ddHHmm')
var managedIdentityName = 'mid-expensemgmt-${timestamp}-${uniqueSuffix}'

// User-Assigned Managed Identity
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: managedIdentityName
  location: location
}

// Outputs
output managedIdentityId string = managedIdentity.id
output managedIdentityClientId string = managedIdentity.properties.clientId
output managedIdentityPrincipalId string = managedIdentity.properties.principalId
output managedIdentityName string = managedIdentity.name
