using System;
using System.Collections.Generic;
using System.Linq;

namespace NullZustand
{
    public class LocationUpdate
    {
        public long UpdateId { get; set; }
        public string Username { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public DateTime Timestamp { get; set; }

        public LocationUpdate(long updateId, string username, float x, float y, float z)
        {
            UpdateId = updateId;
            Username = username;
            X = x;
            Y = y;
            Z = z;
            Timestamp = DateTime.UtcNow;
        }
    }

    public class LocationUpdateTracker
    {
        private long _nextUpdateId = 1;
        private long _minAvailableUpdateId = 1;
        private readonly object _lock = new object();
        private readonly List<LocationUpdate> _updates = new List<LocationUpdate>();
        private readonly Dictionary<string, Vector3> _currentPositions = new Dictionary<string, Vector3>(StringComparer.OrdinalIgnoreCase);
        private const int MAX_STORED_UPDATES = 1000;

        public long RecordUpdate(string username, float x, float y, float z)
        {
            lock (_lock)
            {
                long updateId = _nextUpdateId++;
                var update = new LocationUpdate(updateId, username, x, y, z);
                _updates.Add(update);

                // Update current position
                if (!_currentPositions.ContainsKey(username))
                {
                    _currentPositions[username] = new Vector3();
                }
                _currentPositions[username].X = x;
                _currentPositions[username].Y = y;
                _currentPositions[username].Z = z;

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

        public Vector3 GetCurrentPosition(string username)
        {
            lock (_lock)
            {
                _currentPositions.TryGetValue(username, out Vector3 position);
                return position;
            }
        }

        public Dictionary<string, Vector3> GetAllCurrentPositions()
        {
            lock (_lock)
            {
                return new Dictionary<string, Vector3>(_currentPositions, StringComparer.OrdinalIgnoreCase);
            }
        }

    }
}

