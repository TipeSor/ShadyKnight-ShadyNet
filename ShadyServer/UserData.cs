using System;
using System.Net.Sockets;
using ShadyShared;

namespace ShadyServer
{
    public class UserData
    {
        internal readonly UserState State = new();
        private byte[] _stateBytes;
        internal byte[] StateBytes
        {
            get => _stateBytes ??= State.Serialize();
            set => _stateBytes = value;
        }
        internal readonly Guid Guid = Guid.NewGuid();
        internal NetworkStream Stream { get; init; }
    }
}
