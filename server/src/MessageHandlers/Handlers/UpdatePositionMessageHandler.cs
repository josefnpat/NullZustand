using System;
using System.Threading.Tasks;

namespace NullZustand.MessageHandlers.Handlers
{
    public class UpdatePositionPayload
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
        public float rotX { get; set; }
        public float rotY { get; set; }
        public float rotZ { get; set; }
        public float rotW { get; set; }
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

        private bool IsValidCoordinate(float value)
        {
            // Check for NaN and infinity
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return false;
            }

            // Check bounds
            if (value < ValidationConstants.MIN_COORDINATE || value > ValidationConstants.MAX_COORDINATE)
            {
                return false;
            }

            return true;
        }

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

            // Validate coordinates
            if (!IsValidCoordinate(payload.x) || !IsValidCoordinate(payload.y) || !IsValidCoordinate(payload.z))
            {
                Console.WriteLine($"[WARNING] Invalid coordinates from {session.Username}: ({payload.x}, {payload.y}, {payload.z})");

                await SendAsync(session, new Message
                {
                    Id = message.Id,
                    Type = MessageTypes.ERROR,
                    Payload = new
                    {
                        code = "INVALID_COORDINATES",
                        message = $"Invalid coordinates. Values must be valid numbers between {ValidationConstants.MIN_COORDINATE} and {ValidationConstants.MAX_COORDINATE}."
                    }
                });
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

            // Update the player's position and rotation
            long updateId = _playerManager.UpdatePlayerPosition(session.Username, payload.x, payload.y, payload.z, 
                payload.rotX, payload.rotY, payload.rotZ, payload.rotW);

            Console.WriteLine($"[POSITION] {session.Username} moved to ({payload.x:F2}, {payload.y:F2}, {payload.z:F2}), rotation: ({payload.rotX:F2}, {payload.rotY:F2}, {payload.rotZ:F2}, {payload.rotW:F2}) [UpdateID: {updateId}]");

            // Send acknowledgment back to client
            await SendResponseAsync(session, message, MessageTypes.UPDATE_POSITION_RESPONSE,
                new { success = true, updateId = updateId });
        }
    }
}

