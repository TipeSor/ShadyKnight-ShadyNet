using System;
using System.Net.Sockets;
using ShadyShared;

namespace ShadyServer
{
    public class UserData
    {
        internal readonly UserState state = new();
        internal readonly Guid guid = Guid.NewGuid();
        internal NetworkStream Stream { get; init; }
    }
}
