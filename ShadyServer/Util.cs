using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace ShadyServer
{
    public static class Util
    {
        public static bool TryParseString<T>(string input, out T value)
        {
            try
            {
                value = (T)(typeof(T) == typeof(bool) ? bool.Parse(input) :
                            typeof(T).IsEnum ? Enum.Parse(typeof(T), input) :
                            Convert.ChangeType(input, typeof(T)));
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"I hate you: {ex}");
            }
            value = default!;
            return false;
        }

        public static bool TryParseString(Type type, string input, out object? value)
        {
            try
            {
                value = type == typeof(bool) ? bool.Parse(input) :
                        type.IsEnum ? Enum.Parse(type, input) :
                        Convert.ChangeType(input, type);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"I hate you: {ex}");
            }
            value = default!;
            return false;
        }

        public static string[] ParseInput(string input)
        {
            MatchCollection matches = Regex.Matches(input, @"[\""].+?[\""]|[^ ]+");
            return [.. matches.Cast<Match>().Select(static m =>
            {
                string s = m.Value;
                return s.StartsWith("\"") && s.EndsWith("\"")
                    ? s.Substring(1, s.Length - 2)
                    : s;
            })];
        }
    }
}
