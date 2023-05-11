# Serverless Tic Tac Toe

This is a sample of a serverless multiplayer game running in Azure using the following technologies:

* [Static Web Apps](https://azure.microsoft.com/en-us/products/app-service/static)
* [Azure SignalR Service](https://azure.microsoft.com/en-us/products/signalr-service)
* [(Durable) Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-overview?tabs=csharp-inproc)

![Communication flow](https://www.websequencediagrams.com/cgi-bin/cdraw?lz=Q2xpZW50LT5GdW5jdGlvbnM6IFtTaWduYWxSXSBDcmVhdGVHYW1lCgAXCS0-T3JjaGVzdHJhdG9yOiBTY2hlZHVsZU5ld0luc3RhbmNlKGNvbm5lAEoFSWQpCm5vdGUgcmlnaHQgb2YgADAOAFwGIGdhbWUKAEwMAIEDDkFjdGl2aXR5XTogSm9pbkdhbQBWDiwAPAVJZCkAgRsMAIFUBjogR2FtZUpvaW5lZCgAIAYsICJYIgB5HldhaXRGb3JFdmVudCgiUGxheWVyAD4GIikKAIIWHQCBFwkAgH8TAIIrDlJhaXNlAE4ULCAAgjIOAIEta08AgXkfU3RhcnQAgwMrVXBkYXQAgXcVAIMWCAAXCykKCmxvb3Agd2hpbGUgIWdhbWUuSXNPdmVyAIMBK01vdmUAgwQgTWFrZU1vdgCCdS8AVQUAgwwQAIVAHACBdwYAgVRaZW5kCg&s=default)


## Local development: 

You can use the following emulators for local development:
* [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio) (should start automatically when you launch the `TicTacToe.Functions` project)
* [Azure SignalR Emulator](https://github.com/Azure/azure-signalr/blob/dev/docs/emulator.md)

```cmd
dotnet tool install  -g Microsoft.Azure.SignalR.Emulator
cd .\src\TicTacToe.Functions
asrs-emulator start
```

## Deployment

If you want to deploy the sample to your own Azure subscription:

* Start by forking this repository

* Create a resource group in your Azure subscription

* Then you can use the deployment script in the `iac` folder to create the necessary resources:

    ```cmd
    az deployment group create --resource-group my-sample --template-file iac/main.bicep --name samples-serverless-tictactoe
    ```

* You need to provide your repository URL and a GitHub access token as parameters (the command above will ask for them). You can create an access token [here](https://github.com/settings/tokens?type=beta) (it requires `Read access to metadata` and `Read and Write access to actions, actions variables, code, secrets, and workflows` permissions for your repository).

* Running the command will (among others) create a Function App and a Static Web App in your resource group and create a GitHub action for the Static Web App in your repository.

* Replace the URL in [appsettings.json](src/TicTacToe/TicTacToe.Blazor/wwwroot/appsettings.json) with the URL of your Function App (keep the `/api` postfix).

* Download the publish profile for the Function App that was created.

* Create a new repository secret named `AZURE_FUNCTIONAPP_PUBLISH_PROFILE` and paste the XML code from the publish profile as the value.

* Then you can copy the code from the [existing GitHub action](.github/workflows/azure-static-web-apps-red-water-002751303.yml) to the new action (but replace `AZURE_STATIC_WEB_APPS_API_TOKEN_AMBITIOUS_RIVER_0FBF49203` with the secret name from your file).

* You can now delete the [existing GitHub action](.github/workflows/azure-static-web-apps-red-water-002751303.yml).

* Running the GitHub action should now build and deploy the code and you should be able to run the game by accessing your Static Web App.

* Have fun!