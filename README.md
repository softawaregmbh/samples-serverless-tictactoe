# samples-serverless-tictactoe

## SignalR emulator for local testing
https://github.com/Azure/azure-signalr/blob/dev/docs/emulator.md

```cmd
asrs-emulator start
```

## Bicep deployment

```cmd
az deployment group create --resource-group samples-serverless-tictactoe --template-file iac/main.bicep --name samples-serverless-tictactoe
```