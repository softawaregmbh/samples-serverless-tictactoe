param location string = resourceGroup().location

param signalRName string
param functionAppName string
param swaName string

resource functionApp 'Microsoft.Web/sites@2020-12-01' existing = {
  name: functionAppName
}

resource staticWebApp 'Microsoft.Web/staticSites@2021-01-15' existing = {
  name: swaName
}

resource signalR 'Microsoft.SignalRService/signalR@2022-02-01' = {
  name: signalRName
  location: location
  sku: {
    capacity: 1
    name: 'Free_F1'
    tier: 'Free'
  }
  kind: 'SignalR'
  properties: {
    tls: {
      clientCertEnabled: false
    }
    features: [
      {
        flag: 'ServiceMode'
        value: 'Serverless'
      }
      {
        flag: 'EnableConnectivityLogs'
        value: 'true'
      }
      {
        flag: 'EnableMessagingLogs'
        value: 'true'
      }
      {
        flag: 'EnableLiveTrace'
        value: 'true'
      }
    ]
    cors: {
      allowedOrigins: [
        'https://${staticWebApp.properties.defaultHostname}'
      ]
    }
    publicNetworkAccess: 'Enabled'
    networkACLs: {
      defaultAction: 'Deny'
      publicNetwork: {
        allow: [
          'ServerConnection'
          'ClientConnection'
          'RESTAPI'
          'Trace'
        ]
      }
    }
    upstream: {
      templates: [
        {
          categoryPattern: '*'
          eventPattern: '*'
          hubPattern: '*'
          urlTemplate: 'https://${functionApp.properties.defaultHostName}/runtime/webhooks/signalr?code=${listKeys(resourceId('Microsoft.Web/sites/host', functionAppName, 'default'), '2022-03-01').systemkeys.signalr_extension}'
        }
      ]
    }
  }
}

var signalRServiceOwnerRoleId = '7e4f1700-ea5a-4f59-8f37-079cfe29dce3'
resource signalRServiceOwnerRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(signalRName, functionAppName, signalRServiceOwnerRoleId)
  scope: signalR
  properties: {
      principalId: functionApp.identity.principalId
      principalType: 'ServicePrincipal'
      roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', signalRServiceOwnerRoleId)
  }
}
