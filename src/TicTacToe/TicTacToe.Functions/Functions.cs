using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;

namespace TicTacToe.Functions
{
    public class Functions
    {
        public record JoinInfo(string ConnectionId, string GameId, Player Player);

        public record MoveEvent(string ConnectionId, int Row, int Column);

        public record Result()
        {
            [SignalROutput(HubName = "%HubName%")]
            public SignalRMessageAction[]? Messages { get; set; }

            [SignalROutput(HubName = "%HubName%")]
            public SignalRGroupAction[]? GroupActions { get; set; }
        }

        [Function(nameof(Http_Negotiate))]
        public string Http_Negotiate(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "negotiate")] HttpRequestData request,
            [SignalRConnectionInfoInput(HubName = "%HubName%")] string connectionInfo)
        {
            return connectionInfo;
        }

        [Function(nameof(SignalR_CreateGame))]
        public static async Task SignalR_CreateGame(
            [SignalRTrigger("%HubName%", "messages", "CreateGame")] SignalRInvocationContext invocationContext,
            [DurableClient] DurableTaskClient durableTaskClient)
        {
            var gameId = await durableTaskClient.ScheduleNewOrchestrationInstanceAsync(nameof(Orchestrator));

            await durableTaskClient.RaiseEventAsync(gameId, "PlayerJoined", invocationContext.ConnectionId);
        }

        [Function(nameof(SignalR_JoinGame))]
        public static async Task SignalR_JoinGame(
            [SignalRTrigger("%HubName%", "messages", "JoinGame", nameof(gameId))] SignalRInvocationContext invocationContext,
            string gameId,
            [DurableClient] DurableTaskClient durableTaskClient)
        {
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
            await durableTaskClient.RaiseEventAsync(id, "Move", new MoveEvent(invocationContext.ConnectionId, row, column));
        }

        [Function(nameof(Orchestrator))]
        public static async Task Orchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var logger = context.CreateReplaySafeLogger<Functions>();

            var game = new Game { Id = context.InstanceId };

            var playerXConnection = await context.WaitForExternalEvent<string>("PlayerJoined");
            
            await context.CallActivityAsync(nameof(Activity_JoinGame), new JoinInfo(playerXConnection, game.Id, Player.X));

            var playerOConnection = await context.WaitForExternalEvent<string>("PlayerJoined");
            
            await context.CallActivityAsync(nameof(Activity_JoinGame), new JoinInfo(playerOConnection, game.Id, Player.O));
            
            game.Start();

            await context.CallActivityAsync(nameof(Activity_SendUpdate), game);

            while (!game.IsOver)
            {
                var moveEvent = await context.WaitForExternalEvent<MoveEvent>("Move");
                var player = moveEvent.ConnectionId == playerXConnection ? Player.X : Player.O;
                var move = new Move(player, moveEvent.Row, moveEvent.Column);
                
                if (game.TryMakeMove(move))
                {
                    await context.CallActivityAsync(nameof(Activity_SendUpdate), game);
                }
            }
        }

        [Function(nameof(Activity_JoinGame))]
        public static Result Activity_JoinGame([ActivityTrigger] JoinInfo info)
        {
            return new Result()
            {
                GroupActions = new SignalRGroupAction[]
                {
                    new SignalRGroupAction(SignalRGroupActionType.Add)
                    {
                        ConnectionId = info.ConnectionId,
                        GroupName = info.GameId
                    }
                },
                Messages = new SignalRMessageAction[]
                {
                    new SignalRMessageAction("GameJoined", new object[] { info.GameId, info.Player })
                    {
                        ConnectionId = info.ConnectionId
                    }
                }
            };
        }

        [Function(nameof(Activity_SendUpdate))]
        [SignalROutput(HubName = "%HubName%")]
        public static SignalRMessageAction Activity_SendUpdate([ActivityTrigger]Game game)
        {
            return new SignalRMessageAction("Update", new object[] { game })
            {
                GroupName = game.Id
            };
        }
    }
}