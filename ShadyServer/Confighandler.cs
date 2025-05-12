using System;
using System.Collections.Generic;
using System.Net;
using ShadyShared;

namespace ShadyServer
{
    public static class Config
    {
        private static readonly Dictionary<string, object> config = new()
        {
            { "address", IPAddress.Any},
            { "port", (ushort)8080 }
        };

        public static IPAddress Address => (IPAddress)config["address"];
        public static ushort Port => (ushort)config["port"];

        public static void ParseConfig(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (!args[i].StartsWith("--", StringComparison.Ordinal))
                {
                    continue;
                }

                string name = args[i][2..];
                if (!config.TryGetValue(name, out object originalValue) || !(i + 1 < args.Length))
                {
                    continue;
                }

                Type type = originalValue.GetType();
                if (!Utils.TryParseString(type, args[i + 1], out object value))
                {
                    continue;
                }

                config[name] = value ?? originalValue;
            }
        }
    }
}
