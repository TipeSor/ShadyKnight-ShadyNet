using System;

namespace ShadyServer
{
    public static class Logger
    {
        public static void LogInfo(string data)
        {
            Console.WriteLine($"[INFO] {data}");
        }
    }
}
