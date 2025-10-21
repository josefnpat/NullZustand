using UnityEngine;

public class PlayerState
{
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }
    public float Velocity { get; set; }
    public long TimestampMs { get; set; }

    public PlayerState()
    {
        Position = Vector3.zero;
        Rotation = Quaternion.identity;
        Velocity = 0f;
        TimestampMs = 0L;
    }

    public PlayerState(Vector3 position, Quaternion rotation, float velocity, long timestampMs)
    {
        Position = position;
        Rotation = rotation;
        Velocity = velocity;
        TimestampMs = timestampMs;
    }
}

