param functionAppName string
param keyName string

resource signalRKey 'Microsoft.Web/sites/host/systemkeys@2021-03-01' = {
  name: '${functionAppName}/default/${keyName}'
  properties: {
    name: keyName
  }
}
