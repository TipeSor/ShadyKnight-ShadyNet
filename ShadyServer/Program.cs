using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ShadyServer
{
    public static class Program
    {
        public static TcpListener server;
        public static bool isRunning;

        public static ConcurrentDictionary<TcpClient, UserData> users = new();
        public static Dictionary<string, object> config = new Dictionary<string, object>
        {
            { "address", IPAddress.Loopback },
            { "port", 8080 },
        };

        public static void Main(string[] args)
        {
            ParseConfig(args);
            CommandHandler.RegisterCommands();
            StartServer();
        }

        public static void ParseConfig(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("--"))
                {
                    string name = args[i][2..];
                    if (config.TryGetValue(name, out object originalValue) && i + 1 < args.Length)
                    {
                        Type type = originalValue.GetType();
                        if (Util.TryParseString(type, args[i + 1], out object value))
                        {
                            config[name] = value ?? originalValue;
                        }
                    }
                }
            }
        }

        public static void StartServer()
        {
            try
            {
                Console.Clear();
                IPAddress ip = (IPAddress)config["address"];
                int port = (int)config["port"];

                Logger.LogInfo($"server hosted on {ip}:{port}");

                server = new TcpListener(IPAddress.Loopback, port);
                server.Start();
                isRunning = true;

                _ = Task.Run(static () => ListenForClients());
                _ = Task.Run(static () => SendData());

                while (isRunning)
                {
                    if (Console.ReadKey().KeyChar == 'q')
                    {
                        server.Stop();
                        isRunning = false;
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
            while (isRunning)
            {
                try
                {
                    TcpClient client = await server.AcceptTcpClientAsync();
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

                users[client] = new("name") { writer = writer };

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
                _ = users.TryRemove(client, out _);
                client.Dispose();
            }
        }


        public static async Task SendData()
        {
            while (isRunning)
            {
                foreach (TcpClient client in users.Keys)
                {
                    UserData clientData = users[client];

                    foreach (TcpClient dataClient in users.Keys)
                    {
                        if (dataClient == client)
                        {
                            continue;
                        }

                        Vector3 pos = users[dataClient].position;

                        if (pos == users[dataClient].lastPosition)
                        {
                            continue;
                        }

                        await users[client].writer.WriteLineAsync($"update-data {users[dataClient].guid} {pos.x:F2} {pos.y:F2} {pos.z:F2} {users[dataClient].scene}");
                        users[dataClient].lastPosition = pos;
                    }

                }
                await Task.Delay(30);
            }
        }

        [Command]
        public static void setname(TcpClient client, string name)
        {
            users[client].name = name;
        }

        [Command]
        public static void setposition(TcpClient client, float x, float y, float z, string scene)
        {
            users[client].SetPosition(new(x, y, z));
            users[client].scene = scene;
        }
    }
}
