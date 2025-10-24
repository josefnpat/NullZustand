using System;

namespace NullZustand
{
    public class Entity
    {
        public EntityType Type { get; set; }
        public Vec3 Position { get; set; }
        public Quat Rotation { get; set; }
        public float Velocity { get; set; }
        public long TimestampMs { get; set; }

        public Entity()
        {
            Type = EntityType.Player;
            Position = new Vec3(0, 0, 0);
            Rotation = new Quat(0, 0, 0, 1);
            Velocity = 0f;
            TimestampMs = 0L;
        }

        public Entity(EntityType type, Vec3 position, Quat rotation, float velocity, long timestampMs)
        {
            Type = type;
            Position = position;
            Rotation = rotation;
            Velocity = velocity;
            TimestampMs = timestampMs;
        }
    }
}
