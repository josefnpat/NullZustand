using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NullZustand
{
    public class LocationUpdateEvent : EventArgs
    {
        public long UpdateId { get; set; }
        public Player Player { get; set; }
    }

    public class PlayerManager
    {
        private readonly ConcurrentDictionary<string, Player> _players;
        private readonly LocationUpdateTracker _locationUpdateTracker;
        private readonly EntityManager _entityManager;

        // Event for location updates - allows observers to react without circular dependency
        public event EventHandler<LocationUpdateEvent> LocationUpdated;

        public PlayerManager(EntityManager entityManager)
        {
            _players = new ConcurrentDictionary<string, Player>(StringComparer.OrdinalIgnoreCase);
            _locationUpdateTracker = new LocationUpdateTracker();
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
        }

        public Player GetOrCreatePlayer(string username)
        {
            return _players.GetOrAdd(username, name =>
            {
                var player = new Player(name);
                player.EntityId = _entityManager.CreateEntity();
                Console.WriteLine($"[PLAYER] Created new player: {player} [Total: {_players.Count}]");
                return player;
            });
        }

        public long UpdatePlayerMovement(string username, Quat rotation, float velocity, long serverTimestamp)
        {
            _players.TryGetValue(username, out Player player);
            if (player != null)
            {
                player.UpdateLastSeen();

                Vec3 newPosition;
                Entity oldState = _entityManager.GetEntity(player.EntityId);

                if (oldState != null && oldState.TimestampMs > 0)
                {
                    // Calculate time elapsed since last update (in seconds)
                    float timeDelta = (serverTimestamp - oldState.TimestampMs) / 1000.0f;

                    Vec3 direction = oldState.Rotation.GetForwardVector();
                    Vec3 movement = direction * (oldState.Velocity * timeDelta);
                    newPosition = oldState.Position + movement;

                    Console.WriteLine($"[PLAYER] {username} moved {movement} in {timeDelta:F3}s (old_vel: {oldState.Velocity:F2} -> new_vel: {velocity:F2})");
                }
                else
                {
                    newPosition = new Vec3(0, 0, 0);
                    Console.WriteLine($"[PLAYER] {username} initialized at origin");
                }

                // Update entity in EntityManager
                _entityManager.UpdateEntity(player.EntityId, newPosition, rotation, velocity, serverTimestamp);

                // Record to history tracker
                long updateId = _locationUpdateTracker.RecordUpdate(player);
                Console.WriteLine($"[PLAYER] Updated {username}: pos={newPosition}, rot={rotation}, vel={velocity:F2} [UpdateID: {updateId}]");

                // Raise event for location update (observers can handle broadcasting)
                OnLocationUpdated(new LocationUpdateEvent
                {
                    UpdateId = updateId,
                    Player = player
                });

                return updateId;
            }

            return -1;
        }

        protected virtual void OnLocationUpdated(LocationUpdateEvent evt)
        {
            LocationUpdated?.Invoke(this, evt);
        }

        public List<object> GetAllPlayerLocations()
        {
            return _players.Values.Select(player =>
            {
                var state = _entityManager.GetEntity(player.EntityId);
                if (state == null)
                {
                    return new
                    {
                        username = player.Username,
                        entityId = player.EntityId,
                        x = 0f,
                        y = 0f,
                        z = 0f,
                        rotX = 0f,
                        rotY = 0f,
                        rotZ = 0f,
                        rotW = 1f,
                        velocity = 0f,
                        timestampMs = 0L
                    };
                }
                return new
                {
                    username = player.Username,
                    entityId = player.EntityId,
                    x = state.Position.X,
                    y = state.Position.Y,
                    z = state.Position.Z,
                    rotX = state.Rotation.X,
                    rotY = state.Rotation.Y,
                    rotZ = state.Rotation.Z,
                    rotW = state.Rotation.W,
                    velocity = state.Velocity,
                    timestampMs = state.TimestampMs
                };
            }).Cast<object>().ToList();
        }

        public List<LocationUpdate> GetLocationUpdatesSince(long lastUpdateId)
        {
            return _locationUpdateTracker.GetUpdatesSince(lastUpdateId);
        }

        public long GetCurrentUpdateId()
        {
            return _locationUpdateTracker.GetCurrentUpdateId();
        }

    }
}

