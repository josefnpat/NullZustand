using System;

namespace NullZustand
{
    public class Player
    {
        public string Username { get; }
        public DateTime CreatedAt { get; }
        public DateTime LastSeen { get; set; }

        public Player(string username)
        {
            Username = username ?? throw new ArgumentNullException(nameof(username));
            CreatedAt = DateTime.UtcNow;
            LastSeen = DateTime.UtcNow;
        }

        public void UpdateLastSeen()
        {
            LastSeen = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return $"[Player {Username}] Created: {CreatedAt:u}, Last Seen: {LastSeen:u}";
        }
    }
}

