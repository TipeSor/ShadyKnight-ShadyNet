#pragma warning disable IDE0290
namespace ShadyShared
{
    public readonly struct Vector3
    {
        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        public Vector3(float x, float y, float z) { X = x; Y = y; Z = z; }

        public static implicit operator UnityEngine.Vector3(Vector3 v)
        {
            return new UnityEngine.Vector3(v.X, v.Y, v.Z);
        }

        public static implicit operator Vector3(UnityEngine.Vector3 v)
        {
            return new Vector3(v.x, v.y, v.z);
        }
    }

    public readonly struct Quaternion
    {
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float W { get; }

        public Quaternion(float x, float y, float z, float w) { X = x; Y = y; Z = z; W = w; }

        public static implicit operator UnityEngine.Quaternion(Quaternion q)
        {
            return new UnityEngine.Quaternion(q.X, q.Y, q.Z, q.W);
        }

        public static implicit operator Quaternion(UnityEngine.Quaternion q)
        {
            return new Quaternion(q.x, q.y, q.z, q.w);
        }
    }

    public readonly struct Color
    {
        public float R { get; }
        public float G { get; }
        public float B { get; }
        public float A { get; }

        public Color(float r, float g, float b, float a) { R = r; G = g; B = b; A = a; }

        public static implicit operator UnityEngine.Color(Color c)
        {
            return new UnityEngine.Color(c.R, c.G, c.B, c.A);
        }

        public static implicit operator Color(UnityEngine.Color c)
        {
            return new Color(c.r, c.g, c.b, c.a);
        }
    }
}
