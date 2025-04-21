using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShadyMP
{
    internal class NetworkManager
    {
        internal static NetworkManager Instance { get; } = new();
        internal bool IsConnected => me?.Connected ?? false;

        private TcpClient me;
        private NetworkStream stream;
        private StreamReader reader;
        private StreamWriter writer;

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
                reader = new StreamReader(stream, Encoding.UTF8);
                writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                _ = Task.Run(ServerLoop);
                _ = Task.Run(ClientLoop);
            }
            catch (Exception ex)
            {
                Game.message.Show(ex.ToString());
            }
        }

        internal async Task ServerLoop()
        {
            try
            {
                while (me.Connected)
                {
                    string data = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(data))
                    {
                        break;
                    }
                    string[] splitData = data.Split(' ');
                    string command = splitData[0];
                    string[] args = [.. splitData.Skip(1)];

                    CommandHandler.ExecuteCommand(command, args);
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
            while (me.Connected)
            {
                if (Game.player == null)
                {
                    await Task.Delay(30);
                    continue;
                }

                Vector3 pos = Game.player.t.position;
                string scene = SceneManager.GetActiveScene().name;
                string message = $"setposition {pos.x} {pos.y} {pos.z} {scene}";

                await writer.WriteLineAsync(message);
                await writer.FlushAsync();
                await Task.Delay(30);
            }
        }

        internal void Disconnect()
        {
            reader?.Dispose();
            writer?.Dispose();
            stream?.Dispose();
            me?.Dispose();

            reader = null;
            writer = null;
            stream = null;
            me = null;
        }
    }
}
