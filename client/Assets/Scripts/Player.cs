using System;

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
        EntityId = EntityManager.INVALID_ENTITY_ID;
        Profile = new Profile(username);
    }
}
