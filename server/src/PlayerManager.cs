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

        public long UpdatePlayerPosition(string username, float x, float y, float z, float rotX, float rotY, float rotZ, float rotW)
        {
            _players.TryGetValue(username, out Player player);
            if (player != null)
            {
                player.UpdateLastSeen();
                long updateId = _locationUpdateTracker.RecordUpdate(username, x, y, z, rotX, rotY, rotZ, rotW);
                Vector3 position = _locationUpdateTracker.GetCurrentPosition(username);
                Quaternion rotation = _locationUpdateTracker.GetCurrentRotation(username);
                Console.WriteLine($"[PLAYER] Updated position for {username}: {position}, rotation: {rotation} [UpdateID: {updateId}]");
                return updateId;
            }

            return -1;
        }

        public List<object> GetAllPlayerLocations()
        {
            var positions = _locationUpdateTracker.GetAllCurrentPositions();
            var rotations = _locationUpdateTracker.GetAllCurrentRotations();
            return _players.Keys.Select(username =>
            {
                positions.TryGetValue(username, out Vector3 pos);
                if (pos == null)
                {
                    pos = new Vector3(0, 0, 0); // Default position if not yet set
                }
                rotations.TryGetValue(username, out Quaternion rot);
                if (rot == null)
                {
                    rot = new Quaternion(0, 0, 0, 1); // Default rotation (identity)
                }
                return new
                {
                    username = username,
                    x = pos.X,
                    y = pos.Y,
                    z = pos.Z,
                    rotX = rot.X,
                    rotY = rot.Y,
                    rotZ = rot.Z,
                    rotW = rot.W
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

