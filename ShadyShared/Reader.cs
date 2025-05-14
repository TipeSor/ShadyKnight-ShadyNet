using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private static readonly List<Task> tasks = [];
        public static Task[] Tasks
        {
            get
            {
                Task[] temp;
                lock (tasks)
                {
                    temp = [.. tasks];
                }
                return temp;
            }
        }

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

        public static bool Remove(TcpClient client)
        {

            if (clients.TryRemove(client, out ReadContext context))
            {
                context.Source?.Cancel();
                context.Source?.Dispose();
                return true;
            }
            return false;
        }

        public static async Task Stop()
        {
            if (!IsAcceptingNew)
            {
                return;
            }
            IsAcceptingNew = false;
            foreach (TcpClient client in clients.Keys.ToArray())
            {
                try { _ = Remove(client); }
                catch (Exception ex) { Logger.LogInfo($"{ex}"); }
            }
            await Task.WhenAll(Tasks);
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
            lock (tasks)
            {
                tasks.Add(task);
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
                _ = Remove(context.Client);
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
