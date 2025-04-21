using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace ShadyMP
{
    public static partial class Util
    {
        public static bool TryParseString(Type type, string input, out object value)
        {
            try
            {
                value = type == typeof(bool) ? bool.Parse(input) :
                        Convert.ChangeType(input, type, new CultureInfo("en-US"));
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error parsing: {ex}");
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
    }
}
