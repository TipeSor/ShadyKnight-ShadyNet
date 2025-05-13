using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ShadyShared;
using UnityEngine.SceneManagement;

namespace ShadyMP
{
    internal class NetworkManager
    {
        internal static NetworkManager Instance { get; } = new();
        internal bool IsConnected => me?.Connected ?? false;
        internal bool state = false;

        private TcpClient me;
        private NetworkStream stream;
        private UserState self;

        private CancellationTokenSource source;
        private CancellationToken Token => source.Token;

        private Task writingLoop;
        private Task readingLoop;

        internal NetworkManager() { }

        internal async Task ConnectAsync(string address, int port, string name)
        {
            try
            {
                me ??= new TcpClient();
                await me.ConnectAsync(address, port);

                stream = me.GetStream();
                self = new UserState()
                {
                    Position = Game.player.t.position,
                    SceneName = SceneManager.GetActiveScene().name,
                    UserName = name
                };

                source = new();

                writingLoop = Task.Run(ServerLoop);
                readingLoop = Task.Run(ClientLoop);
                state = true;
            }
            catch (Exception ex)
            {
                Game.message.Show(ex.ToString());
                Plugin.Logger.LogError(ex.ToString());
            }
        }

        internal async Task ServerLoop()
        {
            try
            {
                while (state)
                {
                    if (!stream.DataAvailable)
                    {
                        await Task.Delay(10, Token);
                        continue;
                    }

                    byte[] data = await Protocol.ReadPacketAsync(stream, Token);
                    ProtocolHandler.HandlePacket(data, new());
                }
            }
            catch (TaskCanceledException)
            {
                Plugin.Logger.LogInfo("ServerLoop was canceled");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError(ex);
                _ = Disconnect(); // clean up
            }
        }

        internal async Task ClientLoop()
        {
            try
            {
                while (state)
                {
                    if (Game.player == null)
                    {
                        await Task.Delay(30, Token);
                        continue;
                    }

                    self.Position = Game.player.t.position;
                    self.SceneName = SceneManager.GetActiveScene().name;


                    byte[] data = self.Serialize();
                    byte[] packet = Protocol.BuildPacket(ProtocolID.Server_UpdateState, data);

                    Plugin.Logger.LogInfo($"SENT: {BitConverter.ToString(packet)}");

                    await Protocol.WritePacketAsync(stream, packet, Token);

                    await Task.Delay(30, Token);
                }
            }
            catch (TaskCanceledException)
            {
                Plugin.Logger.LogInfo("Client Loop was canceled");
            }
        }

        internal async Task Disconnect()
        {
            source?.Cancel(false);
            state = false;

            if (readingLoop != null)
            {
                Plugin.Logger.LogInfo("Waiting for reading loop to finish");
                await readingLoop;
                Plugin.Logger.LogInfo("Reading loop finished");
            }

            if (writingLoop != null)
            {
                Plugin.Logger.LogInfo("Waiting for writing loop to finish");
                await writingLoop;
                Plugin.Logger.LogInfo("Writing loop finished");
            }

            if (stream != null && IsConnected)
            {
                Plugin.Logger.LogInfo("Making disconnect packet");
                byte[] packet = Protocol.BuildPacket(ProtocolID.Server_Disconnect, []);
                await Protocol.WritePacketAsync(stream, packet);
                Plugin.Logger.LogInfo("Disconnect packet sent");
            }

            source?.Dispose();
            stream?.Dispose();
            me?.Dispose();
            Plugin.Logger.LogInfo("Networking variables disposed");

            source = null;
            stream = null;
            me = null;
            Plugin.Logger.LogInfo("Networking variables nulled");

            Plugin.Logger.LogInfo("Cleaning up other user objects");
            UserManager.Instance.RemoveUsers();
        }

        [Protocol(ProtocolID.Client_UpdateState)]
        public static void Client_UpdateState(byte[] data, HandlerContext _)
        {
            Guid guid = BitGood.ToGuid(data, 0);
            if (!UserManager.Instance.TryGetUser(guid, out UserData userData))
            {
                Plugin.Logger.LogInfo($"Failed to get user {guid}");
                UserManager.Instance.NewUser(guid);
                return;
            }

            Plugin.Logger.LogInfo($"GOT:  {BitConverter.ToString(data)}");
            userData.state.Deserialize(data, 16);
        }

        [Protocol(ProtocolID.Client_RemoveUser)]
        public static void Client_RemoveUser(byte[] data, HandlerContext _)
        {
            Guid guid = BitGood.ToGuid(data, 0);
            UserManager.Instance.RemoveUser(guid);
        }

        [Protocol(ProtocolID.Client_Test)]
        public static void Client_Test(byte[] data, HandlerContext _)
        {
            string hex = BitConverter.ToString(data);
            Plugin.Logger.LogWarning(hex);
        }
    }
}
