using System;
using System.Collections.Generic;
using System.Linq;

namespace NullZustand
{
    public class LocationUpdate
    {
        public long UpdateId { get; set; }
        public string Username { get; set; }
        public Vec3 Position { get; set; }
        public Quat Rotation { get; set; }
        public float Velocity { get; set; }
        public long TimestampMs { get; set; }
        public DateTime Timestamp { get; set; }

        public LocationUpdate(long updateId, string username, Vec3 position, Quat rotation, float velocity, long timestampMs)
        {
            UpdateId = updateId;
            Username = username;
            Position = position;
            Rotation = rotation;
            Velocity = velocity;
            TimestampMs = timestampMs;
            Timestamp = DateTime.UtcNow;
        }
    }

    public class LocationUpdateTracker
    {
        private long _nextUpdateId = 1;
        private long _minAvailableUpdateId = 1;
        private readonly object _lock = new object();
        private readonly List<LocationUpdate> _updates = new List<LocationUpdate>();
        private readonly Dictionary<string, PlayerState> _currentStates = new Dictionary<string, PlayerState>(StringComparer.OrdinalIgnoreCase);
        private const int MAX_STORED_UPDATES = 1000;

        public long RecordUpdate(string username, Vec3 position, Quat rotation, float velocity, long timestampMs)
        {
            lock (_lock)
            {
                long updateId = _nextUpdateId++;
                var update = new LocationUpdate(updateId, username, position, rotation, velocity, timestampMs);
                _updates.Add(update);

                // Update or create player state
                if (!_currentStates.TryGetValue(username, out PlayerState state))
                {
                    state = new PlayerState(position, rotation, velocity, timestampMs);
                    _currentStates[username] = state;
                }
                else
                {
                    // Update existing state
                    state.Position = position;
                    state.Rotation = rotation;
                    state.Velocity = velocity;
                    state.TimestampMs = timestampMs;
                }

                if (_updates.Count > MAX_STORED_UPDATES)
                {
                    int toRemove = _updates.Count - MAX_STORED_UPDATES;
                    _updates.RemoveRange(0, toRemove);

                    // Update the minimum available update ID after trimming
                    if (_updates.Count > 0)
                    {
                        _minAvailableUpdateId = _updates[0].UpdateId;
                    }

                    Console.WriteLine($"[LOCATION_TRACKER] Trimmed {toRemove} old location updates (min available ID: {_minAvailableUpdateId})");
                }

                return updateId;
            }
        }

        public List<LocationUpdate> GetUpdatesSince(long lastUpdateId)
        {
            lock (_lock)
            {
                // Check if the requested update ID is too old (already trimmed)
                if (lastUpdateId > 0 && lastUpdateId < _minAvailableUpdateId)
                {
                    throw new InvalidOperationException(
                        $"Requested update ID {lastUpdateId} is too old. " +
                        $"Minimum available update ID is {_minAvailableUpdateId}. " +
                        $"Client needs to perform a full resync.");
                }

                return _updates.Where(u => u.UpdateId > lastUpdateId).ToList();
            }
        }

        public long GetCurrentUpdateId()
        {
            lock (_lock)
            {
                return _nextUpdateId - 1;
            }
        }

        public PlayerState GetCurrentState(string username)
        {
            lock (_lock)
            {
                _currentStates.TryGetValue(username, out PlayerState state);
                return state;
            }
        }

        public Dictionary<string, PlayerState> GetAllCurrentStates()
        {
            lock (_lock)
            {
                return new Dictionary<string, PlayerState>(_currentStates, StringComparer.OrdinalIgnoreCase);
            }
        }

    }
}
