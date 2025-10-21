using System;
using System.Threading.Tasks;

namespace NullZustand.MessageHandlers.Handlers
{
    public class UpdatePositionPayload
    {
        public float rotX { get; set; }
        public float rotY { get; set; }
        public float rotZ { get; set; }
        public float rotW { get; set; }
        public float velocity { get; set; }
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

            // Validate rotation quaternion (check for NaN/Infinity)
            if (float.IsNaN(payload.rotX) || float.IsInfinity(payload.rotX) ||
                float.IsNaN(payload.rotY) || float.IsInfinity(payload.rotY) ||
                float.IsNaN(payload.rotZ) || float.IsInfinity(payload.rotZ) ||
                float.IsNaN(payload.rotW) || float.IsInfinity(payload.rotW))
            {
                Console.WriteLine($"[WARNING] Invalid rotation from {session.Username}: ({payload.rotX}, {payload.rotY}, {payload.rotZ}, {payload.rotW})");

                await SendAsync(session, new Message
                {
                    Id = message.Id,
                    Type = MessageTypes.ERROR,
                    Payload = new
                    {
                        code = "INVALID_ROTATION",
                        message = "Invalid rotation. Quaternion components must be valid numbers."
                    }
                });
                return;
            }

            // Validate velocity (must be non-negative)
            if (float.IsNaN(payload.velocity) || float.IsInfinity(payload.velocity) || payload.velocity < 0)
            {
                Console.WriteLine($"[WARNING] Invalid velocity from {session.Username}: {payload.velocity}");

                await SendAsync(session, new Message
                {
                    Id = message.Id,
                    Type = MessageTypes.ERROR,
                    Payload = new
                    {
                        code = "INVALID_VELOCITY",
                        message = "Invalid velocity. Velocity must be a valid non-negative number."
                    }
                });
                return;
            }

            // Create Quat from validated payload
            var rotation = new Quat(payload.rotX, payload.rotY, payload.rotZ, payload.rotW);

            // Server generates timestamp and calculates position
            long serverTimestamp = TimeUtils.GetUnixTimestampMs();

            // Update the player's rotation and velocity (server calculates position)
            long updateId = _playerManager.UpdatePlayerMovement(session.Username, rotation, payload.velocity, serverTimestamp);

            Console.WriteLine($"[POSITION] {session.Username} rotation: {rotation}, velocity: {payload.velocity:F2} [UpdateID: {updateId}]");

            // Send acknowledgment back to client
            await SendResponseAsync(session, message, MessageTypes.UPDATE_POSITION_RESPONSE,
                new { success = true, updateId = updateId });
        }
    }
}

