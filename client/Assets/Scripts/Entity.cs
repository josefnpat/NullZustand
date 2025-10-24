using UnityEngine;
using NullZustand;

public class Entity
{
    public EntityType Type { get; set; }
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }
    public float Velocity { get; set; }
    public long TimestampMs { get; set; }

    public Entity()
    {
        Type = EntityType.Invalid;
        Position = Vector3.zero;
        Rotation = Quaternion.identity;
        Velocity = 0f;
        TimestampMs = 0L;
    }

    public Entity(EntityType type, Vector3 position, Quaternion rotation, float velocity, long timestampMs)
    {
        Type = type;
        Position = position;
        Rotation = rotation;
        Velocity = velocity;
        TimestampMs = timestampMs;
    }
}
