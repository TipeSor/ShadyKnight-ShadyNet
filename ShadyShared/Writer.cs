using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ShadyShared
{
    public static class Writer
    {
        public static readonly ConcurrentQueue<WriteContext> WriteQueue = [];
        public static SemaphoreSlim signal = new(0);

        public static void EnqueueWrite(WriteContext context)
        {
            WriteQueue.Enqueue(context);
            _ = signal.Release();
        }

        public static async Task WriteLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await signal.WaitAsync();
                while (WriteQueue.TryDequeue(out WriteContext context))
                {
                    foreach (NetworkStream reciever in context.Reciever ?? [])
                    {
                        try
                        {
                            await Protocol.WritePacketAsync(reciever, context.Data, token);
                        }
                        catch (TaskCanceledException) when (token.IsCancellationRequested)
                        {
                            Logger.LogInfo("Write loop was canceled");
                            break;
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"{ex}");
                        }
                    }
                }
            }
        }
    }

    public class WriteContext(TcpClient sender, NetworkStream[] receivers, byte[] data)
    {
        public TcpClient Sender { get; } = sender;
        public NetworkStream[] Reciever { get; } = receivers;
        public byte[] Data { get; } = data;
    }
}
