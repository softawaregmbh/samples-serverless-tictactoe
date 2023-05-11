using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;

namespace TicTacToe.Functions;

public class Functions
{
    [Function(nameof(SignalR_CreateGame))]
    public static async Task SignalR_CreateGame(
        [SignalRTrigger("%HubName%", "messages", "CreateGame")] SignalRInvocationContext invocationContext,
        [DurableClient] DurableTaskClient durableTaskClient)
    {
        // Start orchestration
        await durableTaskClient.ScheduleNewOrchestrationInstanceAsync(nameof(Orchestrator), invocationContext.ConnectionId);
    }



    [Function(nameof(SignalR_JoinGame))]
    public static async Task SignalR_JoinGame(
        [SignalRTrigger("%HubName%", "messages", "JoinGame", nameof(gameId))] SignalRInvocationContext invocationContext,
        string gameId,
        [DurableClient] DurableTaskClient durableTaskClient)
    {
        // Raise PlayerJoined event in orchestration
        await durableTaskClient.RaiseEventAsync(gameId, "PlayerJoined", invocationContext.ConnectionId);
    }



    [Function(nameof(SignalR_MakeMove))]
    public static async Task SignalR_MakeMove(
        [SignalRTrigger("%HubName%", "messages", "MakeMove", nameof(id), nameof(row), nameof(column))] SignalRInvocationContext invocationContext,
        string id,
        int row,
        int column,
        [DurableClient] DurableTaskClient durableTaskClient)
    {
        // Raise Move event in orchestration
        await durableTaskClient.RaiseEventAsync(id, "Move", new MoveEvent(invocationContext.ConnectionId, row, column));
    }



    public record MoveEvent(
        string ConnectionId,
        int Row,
        int Column);

    public record JoinParameters(
        string ConnectionId,
        string GameId,
        Player Player);

    public record UpdateParameters(
        Game Game,
        string PlayerXConnection,
        string PlayerOConnection);


    [Function(nameof(Orchestrator))]
    public static async Task Orchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        // When the game is created:
        var game = new Game { Id = context.InstanceId };
        
        var playerXConnection = context.GetInput<string>()!;

        await context.CallActivityAsync(nameof(Activity_JoinGame), new JoinParameters(playerXConnection, game.Id, Player.X));

        var playerOConnection = await context.WaitForExternalEvent<string>("PlayerJoined");



        // When the second player joined:
        await context.CallActivityAsync(nameof(Activity_JoinGame), new JoinParameters(playerOConnection, game.Id, Player.O));

        game.Start();

        var updateParameters = new UpdateParameters(game, playerXConnection, playerOConnection);

        await context.CallActivityAsync(nameof(Activity_SendUpdate), updateParameters);


        
        // Wait for next turn until game is over:
        while (!game.IsOver)
        {
            var moveEvent = await context.WaitForExternalEvent<MoveEvent>("Move");

            var player = moveEvent.ConnectionId == playerXConnection ? Player.X : Player.O;
            var move = new Move(player, moveEvent.Row, moveEvent.Column);

            if (game.TryMakeMove(move))
            {
                await context.CallActivityAsync(nameof(Activity_SendUpdate), updateParameters);
            }
        }
    }



    [Function(nameof(Activity_JoinGame))]
    [SignalROutput(HubName = "%HubName%")]
    public static SignalRMessageAction Activity_JoinGame([ActivityTrigger] JoinParameters parameters)
    {
        // Send GameJoined message to player
        return new SignalRMessageAction("GameJoined", new object[] { parameters.GameId, parameters.Player })
        {
            ConnectionId = parameters.ConnectionId
        };
    }



    [Function(nameof(Activity_SendUpdate))]
    [SignalROutput(HubName = "%HubName%")]
    public static SignalRMessageAction[] Activity_SendUpdate([ActivityTrigger] UpdateParameters parameters)
    {
        // Send Update message to both players
        var parameterArray = new object[] { parameters.Game };

        return new SignalRMessageAction[]
        {
            new SignalRMessageAction("Update", parameterArray)
            {
                ConnectionId = parameters.PlayerXConnection
            },
            new SignalRMessageAction("Update", parameterArray)
            {
                ConnectionId = parameters.PlayerOConnection
            }
        };
    }



    [Function(nameof(Http_Negotiate))]
    public string Http_Negotiate(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "negotiate")] HttpRequestData request,
    [SignalRConnectionInfoInput(HubName = "%HubName%")] string connectionInfo)
    {
        // Get credentials for the Azure SignalR service and return them to the client
        return connectionInfo;
    }
}