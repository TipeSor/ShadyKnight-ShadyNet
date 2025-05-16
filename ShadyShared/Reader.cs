using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ShadyShared
{
    public static class Reader
    {
        private static readonly ConcurrentDictionary<TcpClient, ReadContext> clients = [];
        private static readonly ConcurrentDictionary<TcpClient, Task> tasks = [];
        public static event Action<TcpClient>? OnRemove;

        public static bool IsAcceptingNew { get; private set; } = false;

        public static void Add(TcpClient client)
        {
            if (!IsAcceptingNew)
            {
                return;
            }

            ReadContext context = new(client, new());
            if (clients.TryAdd(client, context))
            {
                StartReading(context);
                Logger.LogInfo($"Started reading from {context.Client.Client.RemoteEndPoint}");
            }
        }

        public static async Task<bool> RemoveAsync(TcpClient client)
        {

            if (clients.TryRemove(client, out ReadContext context))
            {
                context.Source?.Cancel();
                if (tasks.TryGetValue(client, out Task task))
                {
                    await task;
                    if (!tasks.TryRemove(client, out _))
                    {
                        Logger.LogWarning($"failed to remove `{client.Client.RemoteEndPoint}` read task");
                    }
                }
                context.Source?.Dispose();
                return true;
            }

            return false;
        }

        public static async Task StopAsync()
        {
            if (!IsAcceptingNew)
            {
                return;
            }
            IsAcceptingNew = false;
            foreach (TcpClient client in clients.Keys.ToArray())
            {
                try { _ = await RemoveAsync(client); }
                catch (Exception ex) { Logger.LogInfo($"{ex}"); }
            }
            if (tasks.Count != 0)
            {
                Logger.LogWarning("not all tasks were stoped");
            }

            tasks.Clear();
        }

        public static void Start()
        {
            IsAcceptingNew = true;
        }

        public static TcpClient[] GetClients()
        {
            return [.. clients.Keys];
        }

        public static ReadContext[] GetContexts()
        {
            return [.. clients.Values];
        }

        private static void StartReading(ReadContext context)
        {
            Task task = Task.Run(() => ClientReadLoop(context));
            if (!tasks.TryAdd(context.Client, task))
            {
                Logger.LogWarning($"failed to add reading loop for `{context.Client.Client.RemoteEndPoint}` to tasks");
            }
        }

        private static async Task ClientReadLoop(ReadContext context)
        {
            try
            {
                while (!context.Token.IsCancellationRequested)
                {
                    byte[] packet = await Protocol.ReadPacketAsync(context.Stream, context.Token);
                    (ProtocolID id, byte[] data) = Protocol.ParsePacket(packet);
                    ProtocolHandler.Dispatch(id, data, new(context.Client));
                }
            }
            catch (TaskCanceledException) when (context.Token.IsCancellationRequested)
            {
                Logger.LogInfo($"Client read was cancelled");
            }
            catch (EndOfStreamException)
            {
                Logger.LogWarning($"EOS from {context.Client.Client.RemoteEndPoint}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"{ex}");
            }
            finally
            {
                _ = RemoveAsync(context.Client);
                OnRemove?.Invoke(context.Client);
            }
        }

        public class ReadContext(TcpClient client, CancellationTokenSource source)
        {
            public TcpClient Client { get; } = client;
            public NetworkStream Stream { get; } = client.GetStream();
            public CancellationTokenSource Source { get; } = source;
            public CancellationToken Token => Source.Token;
        }
    }
}
