param location string = resourceGroup().location

@description('The globally unique name of the SignalR resource to create.')
param signalRName string = '${resourceGroup().name}-signalR-${uniqueString(resourceGroup().id)}'

@description('The globally unique name of the storage account to create.')
param storageAccountName string = '${resourceGroup().name}storage${uniqueString(resourceGroup().id)}'

@description('The name of the function app')
param functionAppName string = '${resourceGroup().name}-functions'

@description('The name of the static web app')
param swaName string = '${resourceGroup().name}-swa'

@description('The GitHub token for the repository with the following permissions: "Read access to metadata" and "Read and Write access to actions, actions variables, code, secrets, and workflows" (used for creating the static web app). See https://github.blog/2022-10-18-introducing-fine-grained-personal-access-tokens-for-github/.')
@secure()
param gitHubToken string

resource staticWebApp 'Microsoft.Web/staticSites@2021-01-15' = {
    name: swaName
    location: location
    sku: {
      name: 'Standard'
      size: 'Standard'
    }
    properties: {
      branch: 'main'
      repositoryToken: gitHubToken
      repositoryUrl: 'https://github.com/softawaregmbh/samples-serverless-tictactoe'
      buildProperties: {
        appLocation: '/src/TicTacToe/TicTacToe.Blazor'
        outputLocation: 'wwwroot'
      }
    }
    tags: null
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-09-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'Storage'
}

resource consumptionPlan 'Microsoft.Web/serverfarms@2021-03-01' = {
  name: '${functionAppName}-plan'
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${functionAppName}-ai'
  location: location
  kind: 'web'
  properties: { 
    Application_Type: 'web'
    Request_Source: 'rest'
  }
}

resource functionApp 'Microsoft.Web/sites@2020-12-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    httpsOnly: true
    serverFarmId: consumptionPlan.id
    siteConfig: {
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      cors: {
        allowedOrigins: ['https://${staticWebApp.properties.defaultHostname}']
      }
      appSettings: [
        {
          name: 'AzureWebJobsStorage__accountName'
          value: storageAccountName
        }
        {
          name: 'AzureSignalRConnectionString__serviceUri'
          value: 'https://${signalRName}.service.signalr.net'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsights.properties.InstrumentationKey
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
      ]
    }
  }
}

// see https://stackoverflow.com/questions/72368709/azure-function-apps-signalr-extension-is-not-populated-to-use-in-signalr-upstre
var signalRKeyName = 'signalr_extension'
module signalRKey 'signalRKey.bicep' = {
  name: '${functionAppName}-systemkey-${signalRKeyName}'
  params: {
    functionAppName: functionAppName
    keyName: signalRKeyName
  }
  dependsOn:[
    functionApp
  ]
}

module signalR 'signalR.bicep' = {
  name: signalRName
  params: {
    location: location
    functionAppName: functionAppName
    signalRName: signalRName
    swaName: swaName
  }
  dependsOn:[
    signalRKey
  ]
}

var storageBlobDataOwnerRoleId = 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b'
resource storageAccountRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccountName, functionAppName, storageBlobDataOwnerRoleId)
  scope: storageAccount
  properties: {
      principalId: functionApp.identity.principalId
      principalType: 'ServicePrincipal'
      roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataOwnerRoleId)
  }
}

var storageQueueDataContributorRoleId = '974c5e8b-45b9-4653-ba55-5f855dd0fb88'
resource storageQueueDataContributorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccountName, functionAppName, storageQueueDataContributorRoleId)
  scope: storageAccount
  properties: {
      principalId: functionApp.identity.principalId
      principalType: 'ServicePrincipal'
      roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageQueueDataContributorRoleId)
  }
}

var storageTableDataContributorRoleId = '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'
resource storageTableDataContributorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccountName, functionAppName, storageTableDataContributorRoleId)
  scope: storageAccount
  properties: {
      principalId: functionApp.identity.principalId
      principalType: 'ServicePrincipal'
      roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageTableDataContributorRoleId)
  }
}
