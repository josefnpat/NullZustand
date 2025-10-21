using System;
using System.Linq;
using System.Threading.Tasks;

namespace NullZustand.MessageHandlers.Handlers
{
    public class GetLocationUpdatesRequestPayload
    {
        public long lastUpdateId { get; set; }
    }

    public class GetLocationUpdatesMessageHandler : MessageHandler
    {
        private readonly PlayerManager _playerManager;

        public GetLocationUpdatesMessageHandler(PlayerManager playerManager)
        {
            _playerManager = playerManager ?? throw new ArgumentNullException(nameof(playerManager));
        }

        public override string MessageType => MessageTypes.LOCATION_UPDATES_REQUEST;

        // Requires authentication - only logged in players can get updates
        public override bool RequiresAuthentication => true;

        public override async Task HandleAsync(Message message, ClientSession session)
        {
            var payload = GetPayload<GetLocationUpdatesRequestPayload>(message);

            if (payload == null)
            {
                Console.WriteLine($"[WARNING] GetLocationUpdatesRequest received with null payload from {session.SessionId}");
                return;
            }

            long lastUpdateId = payload.lastUpdateId;

            try
            {
                // Get all location updates since the specified ID
                var updates = _playerManager.GetLocationUpdatesSince(lastUpdateId);
                long currentUpdateId = _playerManager.GetCurrentUpdateId();

                // Convert to a simple format for the response
                var updatesList = updates.Select(u => new
                {
                    updateId = u.UpdateId,
                    username = u.Username,
                    x = u.Position.X,
                    y = u.Position.Y,
                    z = u.Position.Z,
                    rotX = u.Rotation.X,
                    rotY = u.Rotation.Y,
                    rotZ = u.Rotation.Z,
                    rotW = u.Rotation.W,
                    velocity = u.Velocity,
                    timestampMs = u.TimestampMs,
                    timestamp = u.Timestamp.ToString("o") // ISO 8601 format
                }).ToList();

                Console.WriteLine($"[LOCATION_UPDATES] Sending {updatesList.Count} updates to {session.Username} (current ID: {currentUpdateId})");

                // Send the response
                await SendResponseAsync(session, message, MessageTypes.LOCATION_UPDATES_RESPONSE, new
                {
                    updates = updatesList,
                    lastLocationUpdateId = currentUpdateId
                });
            }
            catch (InvalidOperationException ex)
            {
                // Client requested updates that are too old (already trimmed)
                Console.WriteLine($"[LOCATION_UPDATES] Client {session.Username} requested trimmed data: {ex.Message}");

                // Send error response with full resync data
                long currentUpdateId = _playerManager.GetCurrentUpdateId();
                var allPlayers = _playerManager.GetAllPlayerLocations();

                await SendAsync(session, new Message
                {
                    Id = message.Id,
                    Type = MessageTypes.ERROR,
                    Payload = new
                    {
                        code = "RESYNC_REQUIRED",
                        message = "Your location data is too old. Performing full resync.",
                        allPlayers = allPlayers,
                        lastLocationUpdateId = currentUpdateId
                    }
                });
            }
        }
    }
}

