using System;
using System.Linq;
using System.Text;

namespace ShadyShared
{
    public static class BitGood
    {
        public static byte[] GetBytes(bool value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return bytes;
        }

        public static bool ToBool(byte[] value, int startIndex)
        {
            byte[] bytes = ExtractBytes(value, startIndex, 1);
            bytes = FixBytes(bytes);
            return BitConverter.ToBoolean(bytes, 0);
        }

        public static byte[] GetBytes(char value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return FixBytes(bytes);
        }

        public static char ToChar(byte[] value, int startIndex)
        {
            byte[] bytes = ExtractBytes(value, startIndex, 2);
            bytes = FixBytes(bytes);
            return BitConverter.ToChar(bytes, 0);
        }

        public static byte[] GetBytes(double value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return FixBytes(bytes);
        }

        public static double ToDouble(byte[] value, int startIndex)
        {
            byte[] bytes = ExtractBytes(value, startIndex, 8);
            bytes = FixBytes(bytes);
            return BitConverter.ToDouble(bytes, 0);
        }

        public static byte[] GetBytes(short value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return FixBytes(bytes);
        }

        public static short ToShort(byte[] value, int startIndex)
        {
            byte[] bytes = ExtractBytes(value, startIndex, 2);
            bytes = FixBytes(bytes);
            return BitConverter.ToInt16(bytes, 0);
        }

        public static byte[] GetBytes(int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return FixBytes(bytes);
        }

        public static int ToInt(byte[] value, int startIndex)
        {
            byte[] bytes = ExtractBytes(value, startIndex, 4);
            bytes = FixBytes(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        public static byte[] GetBytes(long value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return FixBytes(bytes);
        }

        public static long ToLong(byte[] value, int startIndex)
        {
            byte[] bytes = ExtractBytes(value, startIndex, 8);
            bytes = FixBytes(bytes);
            return BitConverter.ToInt64(bytes, 0);
        }

        public static byte[] GetBytes(float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return FixBytes(bytes);
        }

        public static float ToFloat(byte[] value, int startIndex)
        {
            byte[] bytes = ExtractBytes(value, startIndex, 4);
            bytes = FixBytes(bytes);
            return BitConverter.ToSingle(bytes, 0);
        }

        public static byte[] GetBytes(ushort value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return FixBytes(bytes);
        }

        public static ushort ToUShort(byte[] value, int startIndex)
        {
            byte[] bytes = ExtractBytes(value, startIndex, 2);
            bytes = FixBytes(bytes);
            return BitConverter.ToUInt16(bytes, 0);
        }

        public static byte[] GetBytes(uint value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return FixBytes(bytes);
        }

        public static uint ToUInt(byte[] value, int startIndex)
        {
            byte[] bytes = ExtractBytes(value, startIndex, 4);
            bytes = FixBytes(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }

        public static byte[] GetBytes(ulong value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return FixBytes(bytes);
        }

        public static ulong ToULong(byte[] value, int startIndex)
        {
            byte[] bytes = ExtractBytes(value, startIndex, 8);
            bytes = FixBytes(bytes);
            return BitConverter.ToUInt64(bytes, 0);
        }

        public static byte[] GetBytes(Guid value)
        {
            byte[] bytes = value.ToByteArray();
            return bytes;
        }

        public static Guid ToGuid(byte[] value, int startIndex)
        {
            byte[] bytes = ExtractBytes(value, startIndex, 16);
            return new Guid(bytes);
        }

        public static byte[] GetBytes(Vector3 value)
        {
            byte[] bytesX = GetBytes(value.X);
            byte[] bytesY = GetBytes(value.Y);
            byte[] bytesZ = GetBytes(value.Z);
            byte[] bytes = Utils.Combine(bytesX, bytesY, bytesZ);
            return bytes;
        }

        public static Vector3 ToVector3(byte[] value, int startIndex)
        {
            byte[] bytes = ExtractBytes(value, startIndex, 12);
            float x = ToFloat(bytes, 0);
            float y = ToFloat(bytes, 4);
            float z = ToFloat(bytes, 8);
            return new Vector3(x, y, z);
        }

        public static byte[] GetBytes(Quaternion value)
        {
            byte[] bytesX = GetBytes(value.X);
            byte[] bytesY = GetBytes(value.Y);
            byte[] bytesZ = GetBytes(value.Z);
            byte[] bytesW = GetBytes(value.W);
            byte[] bytes = Utils.Combine(bytesX, bytesY, bytesZ, bytesW);
            return bytes;
        }

        public static Quaternion ToQuaternion(byte[] value, int startIndex)
        {
            byte[] bytes = ExtractBytes(value, startIndex, 16);
            float x = ToFloat(bytes, 0);
            float y = ToFloat(bytes, 4);
            float z = ToFloat(bytes, 8);
            float w = ToFloat(bytes, 12);
            return new Quaternion(x, y, z, w);
        }

        public static byte[] GetBytes(Color value)
        {
            byte[] bytesR = GetBytes(value.R);
            byte[] bytesG = GetBytes(value.G);
            byte[] bytesB = GetBytes(value.B);
            byte[] bytesA = GetBytes(value.A);
            byte[] bytes = Utils.Combine(bytesR, bytesG, bytesB, bytesA);
            return bytes;
        }

        public static Color ToColor(byte[] value, int startIndex)
        {
            byte[] bytes = ExtractBytes(value, startIndex, 16);
            float r = ToFloat(bytes, 0);
            float g = ToFloat(bytes, 4);
            float b = ToFloat(bytes, 8);
            float a = ToFloat(bytes, 12);
            return new Color(r, g, b, a);
        }

        public static byte[] GetBytes(string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }

        public static string ToString(byte[] bytes, int startIndex, int length)
        {
            return Encoding.UTF8.GetString(bytes, startIndex, length);
        }

        public static byte[] ExtractBytes(byte[] bytes, int index, int length)
        {
            byte[] segment = new byte[length];
            Array.Copy(bytes, index, segment, 0, length);
            return segment;
        }

        public static byte[] FixBytes(byte[] bytes)
        {
            return BitConverter.IsLittleEndian ? bytes : [.. bytes.Reverse()];
        }
    }
}
