using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;

namespace ShadyServer
{
    internal static class CommandHandler
    {
        public delegate void CommandDelegate(object[] args);

        internal class CommandEntry(string name, MethodInfo method)
        {
            internal string name = name;
            internal MethodInfo method = method;
            internal ParameterInfo[] parameters = method.GetParameters();

            public void Invoke(string[] args, TcpClient client)
            {
                try
                {
                    object[] convertedArgs = ConvertArgs(args, client);
                    _ = method.Invoke(null, convertedArgs);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error executing command '{name}': {ex.Message}");
                }
            }

            private object[] ConvertArgs(string[] args, TcpClient client)
            {
                object[] convertedArgs = new object[parameters.Length];
                int argIndex = 0;

                for (int i = 0; i < parameters.Length; i++)
                {
                    Type type = parameters[i].ParameterType;
                    if (type == typeof(TcpClient))
                    {
                        convertedArgs[i] = client;
                    }
                    else
                    {
                        if (argIndex >= args.Length)
                        {
                            throw new ArgumentException($"Missing argument for parameter '{parameters[i].Name}' ({GetCleanTypeName(type)}).");
                        }

                        convertedArgs[i] = type == typeof(bool) ? bool.Parse(args[argIndex]) :
                                       type.IsEnum ? Enum.Parse(type, args[argIndex], true) :
                                       Convert.ChangeType(args[argIndex], type);
                        argIndex++;
                    }
                }

                return convertedArgs;
            }
        }

        private static readonly List<CommandEntry> CommandList = [];

        internal static bool TryGetCommand(string command_name, out CommandEntry output_command)
        {
            output_command = CommandList.FirstOrDefault(c => c.name == command_name);
            return output_command != null;
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
                    if (!Attribute.IsDefined(method_, typeof(Command)))
                    {
                        continue;
                    }

                    try
                    {
                        string name = $"{method_.Name.ToLowerInvariant()}";

                        CommandEntry commandEntry = new(name, method_);
                        CommandList.Add(commandEntry);
                        Logger.LogInfo($"Created command: '{commandEntry.name}'");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Cannot create command: {method_.Name} - {ex.Message}");
                    }
                }
            }
        }

        internal static void ExecuteCommand(string commandName, string[] args, TcpClient client)
        {
            if (!TryGetCommand(commandName, out CommandEntry command))
            {
                Logger.LogInfo($"Command '{commandName}' not found.");
                return;
            }

            command.Invoke(args, client);
        }

        private static string GetCleanTypeName(Type type)
        {
            return type.IsGenericType
                ? $"{type.Name[..type.Name.IndexOf('`')]}<{string.Join(", ", type.GetGenericArguments().Select(GetCleanTypeName))}>"
                : type.Name;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class Command : Attribute
    {
        public Command() { }
    }
}
