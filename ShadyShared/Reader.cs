using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ShadyShared
{
    public class Reader : IDisposable
    {
        private readonly ConcurrentDictionary<TcpClient, CancellationTokenSource> clients = [];
        private bool disposed;

        public event Action<TcpClient, ProtocolID, byte[]>? OnPacketReceived;
        public event Action<TcpClient, Exception>? OnClientDisconnected;

        public void AddClient(TcpClient client)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(Reader));
            }

            CancellationTokenSource source = new();
            if (clients.TryAdd(client, source))
            {
                _ = Task.Run(() => HandleClientAsync(client, source.Token), source.Token);
            }
        }

        public void RemoveClient(TcpClient client)
        {
            if (clients.TryRemove(client, out CancellationTokenSource? source))
            {
                source.Cancel();
                source.Dispose();
                client.Close();
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                while (!token.IsCancellationRequested)
                {
                    byte[] packet = await Protocol.ReadPacketAsync(stream, token).ConfigureAwait(false);
                    (ProtocolID cmd, byte[] data) = Protocol.ParsePacket(packet);
                    OnPacketReceived?.Invoke(client, cmd, data);
                }
            }
            catch (Exception ex)
            {
                OnClientDisconnected?.Invoke(client, ex);
                RemoveClient(client);
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            foreach (TcpClient client in clients.Keys)
            {
                RemoveClient(client);
            }

            clients.Clear();
        }
    }
}
