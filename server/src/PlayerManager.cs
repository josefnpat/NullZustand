using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NullZustand
{
    public class PlayerManager
    {
        private readonly ConcurrentDictionary<string, Player> _players;
        private readonly LocationUpdateTracker _locationUpdateTracker;

        public PlayerManager()
        {
            _players = new ConcurrentDictionary<string, Player>(StringComparer.OrdinalIgnoreCase);
            _locationUpdateTracker = new LocationUpdateTracker();
        }

        public Player GetOrCreatePlayer(string username)
        {
            return _players.GetOrAdd(username, name =>
            {
                var player = new Player(name);
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

                PlayerState currentState = _locationUpdateTracker.GetCurrentState(username);
                Vec3 newPosition;

                if (currentState != null)
                {
                    // Calculate time elapsed since last update (in seconds)
                    float timeDelta = (serverTimestamp - currentState.TimestampMs) / 1000.0f;

                    Vec3 direction = currentState.Rotation.GetForwardVector();
                    Vec3 movement = direction * (currentState.Velocity * timeDelta);
                    newPosition = currentState.Position + movement;

                    Console.WriteLine($"[PLAYER] {username} moved {movement} in {timeDelta:F3}s (old_vel: {currentState.Velocity:F2} -> new_vel: {velocity:F2})");
                }
                else
                {
                    newPosition = new Vec3(0, 0, 0);
                    Console.WriteLine($"[PLAYER] {username} initialized at origin");
                }

                long updateId = _locationUpdateTracker.RecordUpdate(username, newPosition, rotation, velocity, serverTimestamp);
                Console.WriteLine($"[PLAYER] Updated {username}: pos={newPosition}, rot={rotation}, vel={velocity:F2} [UpdateID: {updateId}]");
                
                return updateId;
            }

            return -1;
        }

        public List<object> GetAllPlayerLocations()
        {
            var states = _locationUpdateTracker.GetAllCurrentStates();
            return _players.Keys.Select(username =>
            {
                states.TryGetValue(username, out PlayerState state);
                if (state == null)
                {
                    state = new PlayerState();
                }
                return new
                {
                    username = username,
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

