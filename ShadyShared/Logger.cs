using System;
namespace ShadyShared
{
    public static class Logger
    {
        public static void LogInfo(string data)
        {
            Console.WriteLine($"[INFO] {data}");
        }

        public static void LogWarning(string data)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[Warning] {data}");
            Console.ResetColor();
        }

        public static void LogError(string data)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Error] {data}");
            Console.ResetColor();
        }
    }
}
