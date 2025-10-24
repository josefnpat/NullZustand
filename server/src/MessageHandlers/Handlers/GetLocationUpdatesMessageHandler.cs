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
        private readonly EntityManager _entityManager;

        public GetLocationUpdatesMessageHandler(PlayerManager playerManager, EntityManager entityManager)
        {
            _playerManager = playerManager ?? throw new ArgumentNullException(nameof(playerManager));
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
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
                var entityDictionary = _entityManager.GetAllEntities();
                long currentUpdateId = _playerManager.GetCurrentUpdateId();
                var updatesList = entityDictionary.Select(kvp =>
                {
                    var entity = kvp.Value;
                    var entityId = kvp.Key;

                    return new
                    {
                        updateId = currentUpdateId,
                        entityId = entityId,
                        entityType = entity.Type.ToString(),
                        x = entity.Position.X,
                        y = entity.Position.Y,
                        z = entity.Position.Z,
                        rotX = entity.Rotation.X,
                        rotY = entity.Rotation.Y,
                        rotZ = entity.Rotation.Z,
                        rotW = entity.Rotation.W,
                        velocity = entity.Velocity,
                        timestampMs = entity.TimestampMs
                    };
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
                var entityDictionary = _entityManager.GetAllEntities();
                var resyncEntityData = entityDictionary.Select(kvp => new
                {
                    entityId = kvp.Key,
                    entityType = kvp.Value.Type.ToString(),
                    x = kvp.Value.Position.X,
                    y = kvp.Value.Position.Y,
                    z = kvp.Value.Position.Z,
                    rotX = kvp.Value.Rotation.X,
                    rotY = kvp.Value.Rotation.Y,
                    rotZ = kvp.Value.Rotation.Z,
                    rotW = kvp.Value.Rotation.W,
                    velocity = kvp.Value.Velocity,
                    timestampMs = kvp.Value.TimestampMs
                }).ToList();

                await SendAsync(session, new Message
                {
                    Id = message.Id,
                    Type = MessageTypes.ERROR,
                    Payload = new
                    {
                        code = "RESYNC_REQUIRED",
                        message = "Your location data is too old. Performing full resync.",
                        allEntities = resyncEntityData,
                        lastLocationUpdateId = currentUpdateId
                    }
                });
            }
        }

    }
}

