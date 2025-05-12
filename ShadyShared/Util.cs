using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace ShadyShared
{
    public static class Utils
    {
        public static byte[] Combine(params byte[][] arrays)
        {
            int totalLength = 0;
            foreach (byte[] arr in arrays)
            {
                totalLength += arr.Length;
            }

            byte[] result = new byte[totalLength];
            int offset = 0;

            foreach (byte[] arr in arrays)
            {
                Buffer.BlockCopy(arr, 0, result, offset, arr.Length);
                offset += arr.Length;
            }

            return result;
        }

        public static bool TryParseString(Type type, string input, out object? value)
        {
            try
            {
                value = type == typeof(bool) ? bool.Parse(input) :
                        Convert.ChangeType(input, type, new CultureInfo("en-US"));
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error parsing: {ex}");
            }
            value = default;
            return false;
        }

        public static string[] ParseInput(string input)
        {
            MatchCollection matches = Regex.Matches(input, @"[\""].+?[\""]|[^ ]+");
            return [.. matches.Cast<Match>().Select(static m =>
            {
                string s = m.Value;
                return s.StartsWith("\"") && s.EndsWith("\"")
                    ? s.Substring(1, s.Length-2)
                    : s;
            })];
        }

        public static string GetCleanTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                string genericBase = type.Name.Substring(0, type.Name.IndexOf('`'));
                string genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetCleanTypeName));
                return $"{genericBase}<{genericArgs}>";
            }

            return type.Name;
        }

        public static void WriteBytes(byte[] bytes)
        {
            string hex = BitConverter.ToString(bytes);
            Console.BackgroundColor = ConsoleColor.DarkMagenta;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(hex);
            Console.ResetColor();
        }
    }
}
