﻿using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ShadyShared;
#pragma warning disable CA1707 
namespace ShadyServer
{
    public static class Program
    {
        public static TcpListener Server { get; set; }
        public static bool IsRunning { get; set; }

        public static CancellationTokenSource Source { get; private set; } = new();
        public static CancellationToken Token => Source.Token;

        public static Reader reader;
        public static Writer writer;

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

                writer = new();
                reader.OnPacketReceived += static (TcpClient client, ProtocolID cmd, byte[] data) =>
                {
                    HandlerContext context = new(client);
                    ProtocolHandler.Dispatch(cmd, data, context);
                };
                reader.OnClientDisconnected += static (TcpClient client, Exception ex) =>
                {
                    UserHandler.DisconnectUser(client);
                };

                reader = new();

                _ = Task.Run(static () => ListenForClients());
                _ = Task.Run(static () => UserHandler.MainDataLoop());

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
                    _ = Task.Run(() => reader.AddClient(client));
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error accepting client: {ex.Message}");
                }
            }
        }

        [Protocol(ProtocolID.Server_UpdateState)]
        public static void Server_UpdateState(byte[] data, HandlerContext context)
        {
            TcpClient client = context.Client;
            if (!UserHandler.TryGetData(client, out UserData clientData))
            {
                return;
            }
            clientData.StateBytes = data;
            clientData.State.Deserialize(data, 0);
        }

        [Protocol(ProtocolID.Server_Disconnect)]
        public static void Server_Disconnect(byte[] _, HandlerContext context)
        {
            TcpClient client = context.Client;
            Logger.LogInfo($"user `{client.Client.RemoteEndPoint}` disconnected (user)");
            UserHandler.DisconnectUser(client);
        }

        [Protocol(ProtocolID.Server_Test)]
        public static void Server_Test(byte[] data, HandlerContext context)
        {
            NetworkStream[] users = [.. UserHandler.Users.Keys.Where(u => u != context.Client).Select(u => u.GetStream())];
            WriteContext writeContext = new(context.Client, users, data);
        }
    }
}
