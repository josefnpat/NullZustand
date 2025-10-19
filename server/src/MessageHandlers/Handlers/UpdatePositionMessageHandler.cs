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
            session.Player.UpdatePosition(payload.x, payload.y, payload.z);

            Console.WriteLine($"[POSITION] {session.Username} moved to {session.Player.Position}");

            // Send acknowledgment back to client
            await SendAsync(session, new Message
            {
                Type = MessageTypes.UPDATE_POSITION_RESPONSE,
                Payload = new
                {
                    username = session.Username,
                    x = session.Player.Position.X,
                    y = session.Player.Position.Y,
                    z = session.Player.Position.Z
                }
            });
        }
    }
}

