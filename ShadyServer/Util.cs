using System.Linq;
using System.Text.RegularExpressions;

namespace ShadyServer
{
    public static class Util
    {
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
