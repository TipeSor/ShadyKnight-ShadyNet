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

                Reader.Start();
                Reader.Add(me);

                writingLoop = Writer.WriteLoop(Token);
                state = true;
            }
            catch (Exception ex)
            {
                Game.message.Show(ex.ToString());
                Plugin.Logger.LogError(ex.ToString());
            }
        }

        internal async Task ClientLoop()
        {
            try
            {
                while (!Token.IsCancellationRequested)
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

                    WriteContext context = new(me, [stream], packet);
                    Writer.EnqueueWrite(context);
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

            _ = Reader.StopAsync();

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

            state = false;
            Plugin.Logger.LogInfo("State has been set to false");

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
