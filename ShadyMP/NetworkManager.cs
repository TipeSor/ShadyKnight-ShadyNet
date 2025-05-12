using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using ShadyShared;
using UnityEngine.SceneManagement;

namespace ShadyMP
{
    internal class NetworkManager
    {
        internal static NetworkManager Instance { get; } = new();
        internal bool IsConnected => me?.Connected ?? false;

        private TcpClient me;
        private NetworkStream stream;

        private UserState self;

        internal NetworkManager()
        {

        }

        internal async Task ConnectAsync(string address, int port)
        {
            try
            {
                me ??= new TcpClient();
                await me.ConnectAsync(address, port);

                stream = me.GetStream();
                self = new UserState(Game.player.t.position, SceneManager.GetActiveScene().name);

                _ = Task.Run(ServerLoop);
                _ = Task.Run(ClientLoop);
            }
            catch (Exception ex)
            {
                Game.message.Show(ex.ToString());
                Logger.LogError(ex.ToString());
            }
        }

        internal async Task ServerLoop()
        {
            try
            {
                while (me.Connected)
                {
                    if (!stream.DataAvailable)
                    {
                        continue;
                    }

                    byte[] data = await Protocol.ReadPacketAsync(stream);
                    Utils.WriteBytes(data);
                    ProtocolHandler.HandlePacket(data, new());
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError(ex);
            }
            finally
            {
                Disconnect();
            }
        }

        internal async Task ClientLoop()
        {
            byte[] a = BitGood.GetBytes(123456789);
            byte[] b = Protocol.BuildPacket(ProtocolID.Server_Test, a);
            await Protocol.WritePacketAsync(stream, b);

            Utils.WriteBytes(b);

            while (me.Connected)
            {
                if (Game.player == null)
                {
                    await Task.Delay(30);
                    continue;
                }

                self.Position = Game.player.t.position;
                self.SceneName = SceneManager.GetActiveScene().name;

                byte[] data = self.Serialize();
                byte[] packet = Protocol.BuildPacket(ProtocolID.Server_UpdateState, data);
                await Protocol.WritePacketAsync(stream, packet);

                await Task.Delay(30);
            }
        }

        internal void Disconnect()
        {
            stream?.Dispose();
            me?.Dispose();

            stream = null;
            me = null;
        }

        [Protocol(ProtocolID.Client_UpdateState)]
        public static void Client_UpdateState(byte[] data, HandlerContext _)
        {
            string hex = BitConverter.ToString(data);
            Logger.LogWarning(hex);

            Guid guid = BitGood.ToGuid(data, 0);
            if (!UserManager.Instance.TryGetUser(guid, out UserData userData))
            {
                UserManager.Instance.NewUser(guid);
            }

            userData.state.Deserialize(data, 16);
        }

        [Protocol(ProtocolID.Client_Test)]
        public static void Client_Test(byte[] data, HandlerContext _)
        {
            string hex = BitConverter.ToString(data);
            Logger.LogWarning(hex);
        }
    }
}
