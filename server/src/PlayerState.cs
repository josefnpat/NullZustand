using System;

namespace NullZustand
{
    public class PlayerState
    {
        public Vec3 Position { get; set; }
        public Quat Rotation { get; set; }
        public float Velocity { get; set; }
        public long TimestampMs { get; set; }

        public PlayerState()
        {
            Position = new Vec3(0, 0, 0);
            Rotation = new Quat(0, 0, 0, 1);
            Velocity = 0f;
            TimestampMs = 0L;
        }

        public PlayerState(Vec3 position, Quat rotation, float velocity, long timestampMs)
        {
            Position = position;
            Rotation = rotation;
            Velocity = velocity;
            TimestampMs = timestampMs;
        }
    }
}

