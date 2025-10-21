using System;

namespace NullZustand
{
    public class Vec3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vec3(float x = 0f, float y = 0f, float z = 0f)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vec3 operator +(Vec3 a, Vec3 b)
        {
            return new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vec3 operator *(Vec3 v, float scalar)
        {
            return new Vec3(v.X * scalar, v.Y * scalar, v.Z * scalar);
        }

        public static Vec3 operator *(float scalar, Vec3 v)
        {
            return v * scalar;
        }

        public override string ToString()
        {
            return $"({X:F2}, {Y:F2}, {Z:F2})";
        }
    }

    public class Quat
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float W { get; set; }

        public Quat(float x = 0f, float y = 0f, float z = 0f, float w = 1f)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public Vec3 RotateVector(Vec3 v)
        {
            // q * v * q^-1 (quaternion-vector multiplication)
            // Optimized formula: v + 2 * cross(q.xyz, cross(q.xyz, v) + q.w * v)
            
            Vec3 qVec = new Vec3(X, Y, Z);
            Vec3 cross1 = Cross(qVec, v);
            Vec3 wv = v * W;
            Vec3 cross2 = Cross(qVec, cross1 + wv);
            
            return v + (cross2 * 2f);
        }

        public Vec3 GetForwardVector()
        {
            return RotateVector(new Vec3(0, 0, 1));
        }

        private static Vec3 Cross(Vec3 a, Vec3 b)
        {
            return new Vec3(
                a.Y * b.Z - a.Z * b.Y,
                a.Z * b.X - a.X * b.Z,
                a.X * b.Y - a.Y * b.X
            );
        }

        public override string ToString()
        {
            return $"({X:F2}, {Y:F2}, {Z:F2}, {W:F2})";
        }
    }
}

