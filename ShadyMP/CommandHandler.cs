using System;
using System.Collections.Generic;
using System.Reflection;

namespace ShadyMP
{
    internal static class CommandHandler
    {
        public delegate void CommandDelegate(object[] args);

        internal class CommandEntry(string name, MethodInfo method)
        {
            internal string name = name;
            internal MethodInfo method = method;
            internal ParameterInfo[] parameters = method.GetParameters();

            public void Invoke(string[] args)
            {
                try
                {
                    object[] convertedArgs = ConvertArgs(args);
                    _ = method.Invoke(null, convertedArgs);
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"Error executing command '{name}': {ex.Message}");
                }
            }

            private object[] ConvertArgs(string[] args)
            {
                object[] convertedArgs = new object[parameters.Length];
                if (parameters.Length != args.Length)
                {
                    throw new ArgumentException($"Incorrect amount of arguments: {name}");
                }

                for (int i = 0; i < parameters.Length; i++)
                {
                    Type type = parameters[i].ParameterType;

                    if (!Util.TryParseString(type, args[i], out object value))
                    {
                        throw new Exception($"Argment `{i}` (`{args[i]}`) is not type `{type}`.");
                    }

                    convertedArgs[i] = value;
                }

                return convertedArgs;
            }
        }

        private static readonly Dictionary<string, CommandEntry> Commands = [];

        internal static bool TryGetCommand(string name, out CommandEntry command)
        {
            return Commands.TryGetValue(name, out command);
        }

        internal static void RegisterCommands()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Type[] classes = assembly.GetTypes();
            foreach (Type class_ in classes)
            {
                MethodInfo[] methods = class_.GetMethods(BindingFlags.Public | BindingFlags.Static);
                foreach (MethodInfo method_ in methods)
                {
                    if (!Attribute.IsDefined(method_, typeof(CommandAttribute)))
                    {
                        continue;
                    }

                    try
                    {
                        string name = $"{method_.Name.ToLowerInvariant()}";

                        Commands[name] = new(name, method_);
                    }
                    catch (Exception ex)
                    {
                        Plugin.Logger.LogError($"Cannot create command: {method_.Name} - {ex}");
                    }
                }
            }
        }

        internal static void ExecuteCommand(string commandName, string[] args)
        {
            if (!TryGetCommand(commandName, out CommandEntry command))
            {
                Plugin.Logger.LogWarning($"Command '{commandName}' not found.");
                return;
            }

            command.Invoke(args);
        }

    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CommandAttribute : Attribute
    {
        public CommandAttribute() { }
    }
}
