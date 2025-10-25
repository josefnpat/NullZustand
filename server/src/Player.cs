using System;

namespace NullZustand
{
    public class Player
    {
        public string Username { get; }
        public DateTime CreatedAt { get; }
        public DateTime LastSeen { get; set; }
        public long EntityId { get; set; }
        public Profile Profile { get; set; }

        public Player(string username)
        {
            Username = username ?? throw new ArgumentNullException(nameof(username));
            CreatedAt = DateTime.UtcNow;
            LastSeen = DateTime.UtcNow;
            EntityId = 0;
            Profile = new Profile(username);
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

