using System.Collections.Concurrent;
using System.Linq;
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

                    NetworkStream[] users = [.. Users.Keys.Where(u => u != client).Select(u => u.GetStream())];
                    WriteContext context = new(client, users, packet);

                    Program.writer.Enqueue(context);
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

            NetworkStream[] users = [.. Users.Keys.Select(static u => u.GetStream())];
            WriteContext context = new(client, users, packet) { Dispose = true };
            Program.writer.Enqueue(context);

            Logger.LogInfo($"user {client.Client.RemoteEndPoint} disconnected (server)");
        }
    }
}
