using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading.Tasks;
using ShadyShared;

namespace ShadyServer
{
    public static class UserHandler
    {
        public static ConcurrentDictionary<TcpClient, UserData> Users { get; set; } = new();

        public static bool TryGetData(TcpClient client, out UserData data)
        {
            return Users.TryGetValue(client, out data);
        }

        public static async Task HandleClient(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();

                Users[client] = new() { Stream = stream };

                while (client.Connected)
                {
                    byte[] data = await Protocol.ReadPacketAsync(stream);
                    ProtocolHandler.HandlePacket(data, new([client]));
                }

            }
            catch (System.IO.EndOfStreamException ex)
            {
                if (Config.Verbose)
                {
                    Logger.LogError($"Error handling client {client.Client.RemoteEndPoint}: {ex.Message}");
                    Logger.LogError($"{ex.StackTrace}");
                }
                else
                {
                    Logger.LogError($"EOS exception from {client.Client.RemoteEndPoint}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"{ex.Message}");
                Logger.LogError($"{ex.StackTrace}");
            }
            finally
            {
                Logger.LogInfo($"Client Disconnected: {client.Client.RemoteEndPoint}");
                if (Users.ContainsKey(client))
                {
                    DisconnectUser(client);
                }
            }
        }

        public static async Task MainDataLoop()
        {
            while (Program.IsRunning)
            {
                foreach ((TcpClient client, UserData clientData) in Users)
                {
                    byte[] guidBytes = BitGood.GetBytes(clientData.Guid);
                    byte[] stateBytes = clientData.StateBytes;
                    byte[] data = Utils.Combine(guidBytes, stateBytes);

                    byte[] packet = Protocol.BuildPacket(ProtocolID.Client_UpdateState, data);

                    await WriteHandler.EnqueueWriteAsync(new(WriteType.Share, client, packet));
                }
                await Task.Delay(100);
            }
        }

        public static void DisconnectUser(TcpClient client)
        {
            UserData clientData;
            while (!Users.TryRemove(client, out clientData))
            {
                continue;
            }

            byte[] data = BitGood.GetBytes(clientData.Guid);
            byte[] packet = Protocol.BuildPacket(ProtocolID.Client_RemoveUser, data);

            WriteHandler.EnqueueWrite(new(WriteType.Share, client, packet));
            client.Dispose();
        }
    }
}
