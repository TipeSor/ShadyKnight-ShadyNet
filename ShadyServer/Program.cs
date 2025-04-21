using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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
            CommandHandler.RegisterCommands();
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
                NetworkStream networkStream = client.GetStream();

                StreamReader reader = new(networkStream, Encoding.UTF8);
                StreamWriter writer = new(networkStream, Encoding.UTF8) { AutoFlush = true };

                foreach ((TcpClient userClient, UserData userData) in Users)
                {
                    await writer.WriteLineAsync($"init {userData.GetGuid()} {userData.Scene}");
                }

                Users[client] = new("name") { Writer = writer };

                while (client.Connected)
                {
                    string data = await reader.ReadLineAsync();
                    if (data == null)
                    {
                        break;
                    }

                    string[] parsedData = Util.ParseInput(data);
                    string commandName = parsedData[0];
                    string[] args = [.. parsedData.Skip(1)];

                    CommandHandler.ExecuteCommand(commandName, args, client);
                }

            }
            catch (Exception ex)
            {
                Logger.LogError($"Error handling client {client.Client.RemoteEndPoint}: {ex.Message}");
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
                foreach ((TcpClient client, UserData data) in Users)
                {
                    foreach ((TcpClient userClient, UserData userData) in Users)
                    {
                        if (userClient == client)
                        {
                            continue;
                        }

                        if (userData.Position == userData.LastPosition)
                        {
                            continue;
                        }

                        if (data.Scene != userData.Scene)
                        {
                            continue;
                        }

                        await Users[client].Writer.WriteLineAsync($"update-data {userData}");
                        userData.LastPosition = userData.Position;
                    }

                }
                await Task.Delay(30);
            }
        }

#pragma warning disable IDE1006
        [Command]
        public static void setname(TcpClient client, string name)
        {
            Users[client].Name = name;
        }

        [Command]
        public static void setposition(TcpClient client, float x, float y, float z)
        {
            Users[client].SetPosition(new(x, y, z));
        }

        [Command]
        public static void setScene(TcpClient client, string scene)
        {
            Users[client].Scene = scene;

            Guid guid = Users[client].GetGuid();

            foreach ((TcpClient user, UserData data) in Users)
            {
                if (client == user || data.Writer == null)
                {
                    continue;
                }

                data.Writer.WriteLine($"clientscene {guid} {scene}");
            }
        }
    }
}
