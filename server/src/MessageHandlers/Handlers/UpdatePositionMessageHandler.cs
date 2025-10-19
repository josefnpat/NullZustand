using System;
using System.Threading.Tasks;

namespace NullZustand.MessageHandlers.Handlers
{
    public class UpdatePositionPayload
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
    }

    public class UpdatePositionMessageHandler : MessageHandler
    {
        private readonly PlayerManager _playerManager;

        public UpdatePositionMessageHandler(PlayerManager playerManager)
        {
            _playerManager = playerManager ?? throw new ArgumentNullException(nameof(playerManager));
        }

        public override string MessageType => MessageTypes.UPDATE_POSITION_REQUEST;

        // Requires authentication - only logged in players can update position
        public override bool RequiresAuthentication => true;

        public override async Task HandleAsync(Message message, ClientSession session)
        {
            var payload = GetPayload<UpdatePositionPayload>(message);

            if (payload == null)
            {
                Console.WriteLine($"[WARNING] UpdatePosition received with null payload from {session.SessionId}");
                return;
            }

            if (session.Player == null)
            {
                Console.WriteLine($"[WARNING] UpdatePosition received but session has no player: {session.SessionId}");
                return;
            }

            // Update the player's position
            long updateId = _playerManager.UpdatePlayerPosition(session.Username, payload.x, payload.y, payload.z);

            Console.WriteLine($"[POSITION] {session.Username} moved to ({payload.x:F2}, {payload.y:F2}, {payload.z:F2}) [UpdateID: {updateId}]");

            // Send acknowledgment back to client
            await SendResponseAsync(session, message, MessageTypes.UPDATE_POSITION_RESPONSE,
                new { success = true, updateId = updateId });
        }
    }
}

