using System;
using System.Collections.Generic;
using System.Linq;

namespace NullZustand
{
    public class LocationUpdate
    {
        public long UpdateId { get; set; }
        public Player Player { get; set; }
        public DateTime Timestamp { get; set; }

        public LocationUpdate(long updateId, Player player)
        {
            UpdateId = updateId;
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Timestamp = DateTime.UtcNow;
        }
    }

    public class LocationUpdateTracker
    {
        private long _nextUpdateId = 1;
        private long _minAvailableUpdateId = 1;
        private readonly object _lock = new object();
        private readonly List<LocationUpdate> _updates = new List<LocationUpdate>();
        private readonly Dictionary<string, Player> _currentPlayers = new Dictionary<string, Player>(StringComparer.OrdinalIgnoreCase);
        private const int MAX_STORED_UPDATES = 1000;

        public long RecordUpdate(Player player)
        {
            lock (_lock)
            {
                long updateId = _nextUpdateId++;
                var update = new LocationUpdate(updateId, player);
                _updates.Add(update);

                _currentPlayers[player.Username] = player;

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

        public Player GetCurrentPlayer(string username)
        {
            lock (_lock)
            {
                _currentPlayers.TryGetValue(username, out Player player);
                return player;
            }
        }

        public Dictionary<string, Player> GetAllCurrentPlayers()
        {
            lock (_lock)
            {
                return new Dictionary<string, Player>(_currentPlayers, StringComparer.OrdinalIgnoreCase);
            }
        }

    }
}
