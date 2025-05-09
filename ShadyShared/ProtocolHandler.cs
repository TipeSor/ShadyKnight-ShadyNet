using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#pragma warning disable IDE0290
namespace ShadyShared
{
    public static class ProtocolHandler
    {
        public static readonly Dictionary<uint, HandlerDelegate> ProtocolHandlers = [];
        public delegate void HandlerDelegate(byte[] data, HandlerContext context);

        public static void RegisterHandlers(Assembly? source = null)
        {
            Assembly assembly = source ?? Assembly.GetCallingAssembly();
            foreach (MethodInfo method in assembly.GetTypes()
                    .SelectMany(static t => t.GetMethods(BindingFlags.Static | BindingFlags.Public)))
            {
                Attribute attribute = method.GetCustomAttribute(typeof(ProtocolAttribute));
                if (attribute is not ProtocolAttribute protocolAttribute)
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 2 ||
                    parameters[0].ParameterType != typeof(byte[]) ||
                    parameters[1].ParameterType != typeof(HandlerContext) ||
                    method.ReturnType != typeof(void))
                {
                    Console.WriteLine($"invalid signature for method `{method.DeclaringType.FullName}.{method.Name}` it should be `void(byte[])`");
                    continue;
                }
                ProtocolHandlers[protocolAttribute.ProtocolId] = (HandlerDelegate)method.CreateDelegate(typeof(HandlerDelegate));
            }
        }

        public static void Dispatch(ProtocolID protocolID, byte[] data, HandlerContext context)
        {
            if (ProtocolHandlers.TryGetValue((uint)protocolID, out HandlerDelegate handler))
            {
                handler.Invoke(data, context);
            }
        }

        public static void HandlePacket(byte[] data, HandlerContext context)
        {
            (ProtocolID protocolID, byte[] dataBytes) = Protocol.ParsePacket(data);
            Dispatch(protocolID, dataBytes, context);
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ProtocolAttribute : Attribute
    {
        public uint ProtocolId;

        public ProtocolAttribute(uint protocolId)
        {
            ProtocolId = protocolId;
        }

        public ProtocolAttribute(ProtocolID protocolID)
        {
            ProtocolId = (uint)protocolID;
        }
    }

    public struct HandlerContext
    {
        public object[] objects;

        public HandlerContext(object[] objects)
        {
            this.objects = objects;
        }
    }
}
