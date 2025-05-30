using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ShadyShared
{
    public class Protocol
    {
        public static byte[] BuildPacket(ProtocolID command, byte[] data)
        {
            byte[] cmd = BitGood.GetBytes((uint)command);
            int len = cmd.Length + data.Length;
            byte[] lenBytes = BitGood.GetBytes(len);
            byte[] payload = Utils.Combine(lenBytes, cmd, data);
            return payload;
        }

        public static (ProtocolID, byte[]) ParsePacket(byte[] payload)
        {
            uint protocolNumber = BitGood.ToUInt(payload, 0);
            if (!Enum.IsDefined(typeof(ProtocolID), protocolNumber))
            {
                Logger.LogError($"key `{protocolNumber}` not found in `ProtocolID`");
            }
            ProtocolID command = (ProtocolID)protocolNumber;
            byte[] data = BitGood.ExtractBytes(payload, 4, payload.Length - 4);
            return (command, data);
        }

        public static async Task WritePacketAsync(NetworkStream stream, byte[] data, CancellationToken token = default)
        {
            await stream.WriteAsync(data, 0, data.Length, token);
        }

        public static async Task<byte[]> ReadPacketAsync(NetworkStream stream, CancellationToken token = default)
        {
            byte[] lengthBytes = new byte[4];
            await ReadExactAsync(stream, lengthBytes, 0, 4, token);

            int length = BitGood.ToInt(lengthBytes, 0);
            byte[] data = new byte[length];
            await ReadExactAsync(stream, data, 0, length, token);

            return data;
        }

        public static async Task ReadExactAsync(NetworkStream stream, byte[] buffer, int offset, int count, CancellationToken token = default)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int bytesRead = await stream.ReadAsync(buffer, totalRead + offset, count - totalRead, token);
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException($"stream ended before all bytes were read. expected: `{count}`. got: `{totalRead}`");
                }
                totalRead += bytesRead;
            }
        }
    }

#pragma warning disable IDE0055
    public enum ProtocolID : uint
    {
        Client_InitUser                 = 0x00000000,
        Client_UpdateState              = 0x00000001,
        Client_Test                     = 0x00000002,
        Client_Disconnect               = 0x00000004,
        Client_RemoveUser               = 0x00000005,

        Server_VersionCheck             = 0x80000000,
        Server_UpdateState              = 0x80000001,
        Server_Test                     = 0x80000002,
        Server_Disconnect               = 0x80000003
    }
#pragma warning restore IDE0055
}
