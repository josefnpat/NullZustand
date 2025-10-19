using System;

namespace NullZustand
{
    public class Vector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vector3(float x = 0f, float y = 0f, float z = 0f)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override string ToString()
        {
            return $"({X:F2}, {Y:F2}, {Z:F2})";
        }
    }

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

