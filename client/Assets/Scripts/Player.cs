using System;

public class Player
{
    public string Username { get; }
    public DateTime CreatedAt { get; }
    public DateTime LastSeen { get; set; }
    public PlayerState CurrentState { get; set; }

    public Player(string username)
    {
        Username = username ?? throw new ArgumentNullException(nameof(username));
        CreatedAt = DateTime.UtcNow;
        LastSeen = DateTime.UtcNow;
        CurrentState = new PlayerState();
    }
}
