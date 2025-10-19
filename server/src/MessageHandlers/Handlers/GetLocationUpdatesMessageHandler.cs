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

            // Get all location updates since the specified ID
            var updates = _playerManager.GetLocationUpdatesSince(lastUpdateId);
            long currentUpdateId = _playerManager.GetCurrentUpdateId();

            // Convert to a simple format for the response
            var updatesList = updates.Select(u => new
            {
                updateId = u.UpdateId,
                username = u.Username,
                x = u.X,
                y = u.Y,
                z = u.Z,
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
    }
}

