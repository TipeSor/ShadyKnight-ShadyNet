using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using ShadyShared;

namespace ShadyServer
{
    public static class Program
    {
        public static TcpListener Server { get; set; }
        public static bool IsRunning { get; set; }
        public static ConcurrentDictionary<TcpClient, UserData> Users { get; set; } = new();

        public static void Main(string[] args)
        {
            Config.ParseConfig(args);
            ProtocolHandler.RegisterHandlers();
            StartServer();
        }


        public static void StartServer()
        {
            try
            {
                Console.Clear();
                IPAddress ip = Config.Address;
                int port = Config.Port;

                Logger.LogInfo($"server hosted on {ip}:{port}");

                Server = new TcpListener(ip, port);
                Server.Start();
                IsRunning = true;

                _ = Task.Run(static () => ListenForClients());
                _ = Task.Run(static () => SendData());

                while (IsRunning)
                {
                    if (Console.ReadKey().KeyChar == 'q')
                    {
                        Server.Stop();
                        IsRunning = false;
                        Logger.LogInfo("Server stopped.");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
            }
        }

        public static async Task ListenForClients()
        {
            while (IsRunning)
            {
                try
                {
                    TcpClient client = await Server.AcceptTcpClientAsync();
                    Logger.LogInfo($"Client connected: {client.Client.RemoteEndPoint}");
                    _ = Task.Run(() => HandleClient(client));
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error accepting client: {ex.Message}");
                }
            }
        }

        public static async Task HandleClient(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();

                Users[client] = new() { Stream = stream };

                while (client.Connected)
                {
                    if (!stream.DataAvailable)
                    {
                        await Task.Delay(30);
                        continue;
                    }

                    byte[] data = await Protocol.ReadPacketAsync(stream);
                    ProtocolHandler.HandlePacket(data, new([client]));
                }

            }
            catch (Exception ex)
            {
                Logger.LogError($"Error handling client {client.Client.RemoteEndPoint}: {ex.Message}");
                Logger.LogError($"{ex.StackTrace}");
            }
            finally
            {
                Logger.LogInfo($"Client Disconnected: {client.Client.RemoteEndPoint}");
                _ = Users.TryRemove(client, out _);
                client.Dispose();
            }
        }


        public static async Task SendData()
        {
            while (IsRunning)
            {
                foreach ((TcpClient client, UserData clientData) in Users)
                {
                    foreach ((TcpClient userClient, UserData userData) in Users)
                    {
                        if (userClient == client)
                        {
                            continue;
                        }

                        byte[] guidBytes = BitGood.GetBytes(userData.guid);
                        byte[] stateBytes = userData.state.Serialize();
                        byte[] data = Utils.Combine(guidBytes, stateBytes);

                        byte[] packet = Protocol.BuildPacket(ProtocolID.Client_UpdateState, data);
                        await Protocol.WritePacketAsync(clientData.Stream, packet);
                    }

                }
                await Task.Delay(100);
            }
        }

        [Protocol(ProtocolID.Server_UpdateState)]
        public static void Server_UpdateState(byte[] data, HandlerContext context)
        {
            TcpClient client = (TcpClient)context.objects.FirstOrDefault();
            Users[client].state.Deserialize(data, 0);
        }

        [Protocol(ProtocolID.Server_Test)]
        public static void Server_Test(byte[] data, HandlerContext context)
        {
            TcpClient client = (TcpClient)context.objects.FirstOrDefault();
            foreach ((TcpClient user, UserData userData) in Users)
            {
                if (client == user)
                {
                    continue;
                }

                byte[] packet = Protocol.BuildPacket(ProtocolID.Client_Test, data);
                _ = Protocol.WritePacketAsync(userData.Stream, packet);
            }
        }
    }
}
