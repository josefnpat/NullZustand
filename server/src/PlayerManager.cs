using System;
using System.Collections.Concurrent;

namespace NullZustand
{
    public class PlayerManager
    {
        private readonly ConcurrentDictionary<string, Player> _players;

        public PlayerManager()
        {
            _players = new ConcurrentDictionary<string, Player>(StringComparer.OrdinalIgnoreCase);
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

        public bool UpdatePlayerPosition(string username, float x, float y, float z)
        {
            _players.TryGetValue(username, out Player player);
            if (player != null)
            {
                player.UpdatePosition(x, y, z);
                Console.WriteLine($"[PLAYER] Updated position for {username}: {player.Position}");
                return true;
            }

            return false;
        }

    }
}

