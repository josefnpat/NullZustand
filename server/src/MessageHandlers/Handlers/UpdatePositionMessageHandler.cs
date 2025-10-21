using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

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
        private readonly SessionManager _sessionManager;

        public UpdatePositionMessageHandler(PlayerManager playerManager, SessionManager sessionManager)
        {
            _playerManager = playerManager ?? throw new ArgumentNullException(nameof(playerManager));
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));

            // Subscribe to location updates for broadcasting (similar to ChatMessageHandler pattern)
            _playerManager.LocationUpdated += OnLocationUpdated;
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

            // Check if quaternion is near-zero (degenerate)
            float quatMagnitude = rotation.Magnitude();
            if (quatMagnitude < 0.0001f)
            {
                Console.WriteLine($"[WARNING] Degenerate quaternion from {session.Username}: magnitude={quatMagnitude}");

                await SendAsync(session, new Message
                {
                    Id = message.Id,
                    Type = MessageTypes.ERROR,
                    Payload = new
                    {
                        code = "INVALID_ROTATION",
                        message = "Invalid rotation. Quaternion magnitude is too small."
                    }
                });
                return;
            }

            // Normalize quaternion to ensure unit length
            rotation.Normalize();

            // Server generates timestamp and calculates position
            long serverTimestamp = TimeUtils.GetUnixTimestampMs();

            // Update the player's rotation and velocity (server calculates position)
            long updateId = _playerManager.UpdatePlayerMovement(session.Username, rotation, payload.velocity, serverTimestamp);

            Console.WriteLine($"[POSITION] {session.Username} rotation: {rotation}, velocity: {payload.velocity:F2} [UpdateID: {updateId}]");

            // Send acknowledgment back to client
            await SendResponseAsync(session, message, MessageTypes.UPDATE_POSITION_RESPONSE,
                new { success = true, updateId = updateId });
        }

        private void OnLocationUpdated(object sender, LocationUpdateEvent evt)
        {
            // Broadcast location update to all authenticated clients
            // Run asynchronously to avoid blocking the PlayerManager
            _ = Task.Run(() => BroadcastLocationUpdateAsync(evt));
        }

        private async Task BroadcastLocationUpdateAsync(LocationUpdateEvent evt)
        {
            var sessions = _sessionManager.GetAllAuthenticatedSessions();

            // Send update to each connected client
            foreach (var session in sessions)
            {
                // Synchronize writes to this session's stream
                // Without this, multiple broadcasts can overlap and corrupt the TCP stream:
                //   Player moves → Broadcast starts writing to stream
                //   Player moves again → Another broadcast tries to write SAME stream
                //   Result: Interleaved data → Connection breaks → No more messages
                await session.StreamSemaphore.WaitAsync();

                try
                {
                    // Now we have exclusive access to write to this session's stream
                    string json = JsonConvert.SerializeObject(new Message
                    {
                        Id = Guid.NewGuid().ToString(),
                        Type = MessageTypes.LOCATION_UPDATES_BROADCAST,
                        Payload = new
                        {
                            updateId = evt.UpdateId,
                            username = evt.Username,
                            x = evt.State.Position.X,
                            y = evt.State.Position.Y,
                            z = evt.State.Position.Z,
                            rotX = evt.State.Rotation.X,
                            rotY = evt.State.Rotation.Y,
                            rotZ = evt.State.Rotation.Z,
                            rotW = evt.State.Rotation.W,
                            velocity = evt.State.Velocity,
                            timestampMs = evt.State.TimestampMs
                        }
                    });

                    await MessageFraming.WriteMessageAsync(session.Stream, json);
                    Console.WriteLine($"[BROADCAST] Sent location update for {evt.Username} to {session.SessionId} [UpdateID: {evt.UpdateId}]");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to broadcast location update to {session.SessionId}: {ex.Message}");
                }
                finally
                {
                    // ALWAYS release - even if exception occurred
                    // Ensures other tasks waiting to write can proceed
                    session.StreamSemaphore.Release();
                }
            }
        }
    }
}

